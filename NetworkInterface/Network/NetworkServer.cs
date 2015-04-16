#region Header

/**
 * 
 * Controls outgoing/incoming data for the program.
 * 
 * Major upgrade need: send and received info for which port to use when sending data.
 * 
 **/

#endregion Header

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using P2CCore;
using System.IO;
using P2CCommon;

namespace Network 
{
	public class NetworkServer 
	{

		#region Fields
		// this is how the app will know if there's incoming package waiting to be deliver. 
		public delegate void P2CDeliveryService(Package package, EventArgs e);
		public event P2CDeliveryService P2CDS;

		//switch
		bool hasPackage;							// when NetworkServer received data and finish de-serializing it this turn to true
		bool hasStartedOnce;						// allow for loging back in when user disconnect.

		ConcurrentDictionary<Guid, IPublicProfile>		friendsProfileDict;
		ConcurrentDictionary<Guid, IPEndPoint>			friendsIPaddressDict;
				
		Socket server;

		CancellationTokenSource tokenSource;
		Task serverTask;
		Task[] sendTask;

		Package arrivedPackage;

		IPublicProfile userPublicProfile;

		int defaultPort;

		#endregion Fields


		#region Constructors

		public NetworkServer(IPublicProfile userPublicProfile, int port = 12345, int backlog = 100)
		{	
			this.userPublicProfile = userPublicProfile;

			hasPackage = false;
			hasStartedOnce = false;
				
			friendsProfileDict		= new ConcurrentDictionary<Guid,IPublicProfile>();
			friendsIPaddressDict	= new ConcurrentDictionary<Guid,IPEndPoint>();

			defaultPort = port;

			IPAddress localIP = null;
			foreach(var ip in Dns.GetHostAddresses(Dns.GetHostName())){
				if(ip.AddressFamily == AddressFamily.InterNetwork){
					localIP = ip;
					break;
				}
			}

			server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			server.Bind(new IPEndPoint(localIP, port));
			server.LingerState = new LingerOption(false, 0);
			server.Listen(backlog);			
		}

		#endregion Constructors


		#region Properties


		/// <summary>
		/// Allow user to get the most recent arrived package. Could removed this in the future?
		/// </summary>
		public Package Package
		{
			get
			{
				if(hasPackage)
					return arrivedPackage;
				else{
					return null;						// should we return an empty package?
				}
			}
		}

		#endregion Properties


		#region Methods

		/// <summary>
		/// Start the process in which NetworkServer listen for incoming connection. Runs on a Separate Thread.
		/// </summary>
		public void Start()
		{
			if(serverTask != null)
				return;

			tokenSource = new CancellationTokenSource();
			
			serverTask = Task.Factory.StartNew(()=>{
				CancellationToken cancelToken = tokenSource.Token;
				try{
					while(!cancelToken.IsCancellationRequested){
						Socket client = server.Accept();

						Task.Factory.StartNew(()=>{ ProcessIncomingData(client); });
					}
				}
				catch(SocketException ex){
					Task.Factory.StartNew(()=>{ 
						MessageBox.Show("Inside Server Task: " + Environment.NewLine +
										"Exception: " + ex.Message + Environment.NewLine +
										"Stack Track: " + ex.StackTrace + Environment.NewLine +
										"Exception type: " + ex.GetType().ToString() + Environment.NewLine +
										"Inner Exception: " + ex.InnerException);
					});
				}

				Task.Factory.StartNew(()=>{ MessageBox.Show("Debug inside serverTask. exiting."); });
			});

			if(hasStartedOnce){
				// let friends know user reconnected
				Task.Factory.StartNew(()=>{
					foreach(var friendInfo in friendsIPaddressDict){
						DeliverConnectRequest(friendInfo.Value);
					}
				});
			}

			hasStartedOnce = true;
		}


		/// <summary>
		/// Gracefully disconnect the NetworkServer. Disconnect is instant. Disconnect stop you from receiving data.
		/// </summary>
		public void Disconnect()
		{
			if(serverTask == null)
				return;


			Task.Factory.StartNew(()=>{ SendOther(PackageStatus.LogOff, null); });

			//server.Shutdown(SocketShutdown.Both);
			server.Close();
			//server = null;

			tokenSource.Cancel();

			serverTask = null;
		}

		/// <summary>
		/// Send the package to all connected node. 
		/// </summary>
		/// <param name="status">Allow the program to know the content of the package</param>
		/// <param name="strData">Either it's the message to be sent out or user's old profile nick</param>
		public async void Send(PackageStatus status, string strData)
		{
			if(status == PackageStatus.Message)
				SendMessage(strData);
			else
				SendOther(status, strData);
		}

		async void SendMessage(string strData){
			 // There's a better way to do this so we can catch all the socketException and handle it correctly.
			// When we catch an exception most likely user had disconnected and we need to update that change
			// with the program.			
			IPEndPoint friendIP;
			Socket remoteSocket = null;
			byte[] outgoingData = null;

			try{									
				foreach(var friendInfo in friendsIPaddressDict){
					using(MemoryStream ms = new MemoryStream()){            // might need to handle memory error in the future
						BinaryFormatter bf = new BinaryFormatter();
						friendIP = friendInfo.Value;

						IPublicProfile outgoingProfile;
						friendsProfileDict.TryGetValue(friendInfo.Key, out outgoingProfile);

						Package deliveryPackage = new Package(
							null,
							new Tuple<Guid,string,string>(Guid.Empty, userPublicProfile.UserNick, string.Empty),
							PackageStatus.Message,
							outgoingProfile.Encrypt(Encoding.UTF8.GetBytes(strData)),
							0
						);					

						bf.Serialize(ms, deliveryPackage);
						ms.Seek(0, SeekOrigin.Begin);
						outgoingData = ms.ToArray();
						
						remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
						remoteSocket.Connect(friendIP);
						remoteSocket.Send(outgoingData, 0, outgoingData.Length, SocketFlags.None);
						remoteSocket.Close();
						remoteSocket = null;
						outgoingData = null;
					}
				}
			}
			catch(SocketException se){
				Task.Factory.StartNew(()=>{
					MessageBox.Show("Error while sending data" + Environment.NewLine +
									"Remote ip: " + remoteSocket.RemoteEndPoint.ToString() + Environment.NewLine +
									"Socket Exception: " + Environment.NewLine +
									se.Message + Environment.NewLine +
									"Error Code: " + se.NativeErrorCode + Environment.NewLine);
				});
			
				remoteSocket.Dispose();
				remoteSocket = null;
				outgoingData = null;
			}
			catch(Exception ex){
				Task.Factory.StartNew(()=>{
					MessageBox.Show("Error while sending data" + Environment.NewLine +
									ex.Message + Environment.NewLine);
				});
			}
		}

		async void SendOther(PackageStatus status, string strData){
			// There's a better way to do this so we can catch all the socketException and handle it correctly.
			// When we catch an exception most likely user had disconnected and we need to update that change
			// with the program.			
			IPEndPoint friendIP;	
			byte[] outgoingData = null;
			Socket remoteSocket = null;

			try{			
				using(MemoryStream ms = new MemoryStream()){            // might need to handle memory error in the future
					BinaryFormatter bf = new BinaryFormatter();

					Package deliveryPackage = null;
					if(status == PackageStatus.NickUpdate){
						deliveryPackage = new Package(
							null,
							new Tuple<Guid,string, string>(userPublicProfile.GlobalId, strData, userPublicProfile.UserNick),
							PackageStatus.NickUpdate,
							null,
							0
						);
					}				
					else{ // PackageStatus.LogOff
						  // PackageStatus.Connect is taken care of in ConnectToRemote()
						deliveryPackage = new Package(
							null,
							new Tuple<Guid,string,string>(userPublicProfile.GlobalId, userPublicProfile.UserNick, string.Empty),
							PackageStatus.LogOff,
							null,
							0
						);
					}

					bf.Serialize(ms, deliveryPackage);
					ms.Seek(0, SeekOrigin.Begin);
					outgoingData = ms.ToArray();					
				}				

				foreach(var friendInfo in friendsIPaddressDict){
					friendIP = friendInfo.Value;

					IPublicProfile outgoingProfile;
					friendsProfileDict.TryGetValue(friendInfo.Key, out outgoingProfile);
						
					remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					remoteSocket.Connect(friendIP);
					remoteSocket.Send(outgoingData, 0, outgoingData.Length, SocketFlags.None);
					remoteSocket.Close();
					remoteSocket = null;
				}
			}
			catch(SocketException se){
				Task.Factory.StartNew(()=>{
					MessageBox.Show("Error while sending data" + Environment.NewLine +
									"Remote ip: " + remoteSocket.RemoteEndPoint.ToString() + Environment.NewLine +
									"Socket Exception: " + Environment.NewLine +
									se.Message + Environment.NewLine +
									"Error Code: " + se.NativeErrorCode + Environment.NewLine);
				});

				remoteSocket.Dispose();
				remoteSocket = null;
			}
			catch(Exception ex){
				Task.Factory.StartNew(()=>{
					MessageBox.Show("Error while sending data" + Environment.NewLine +
									ex.Message + Environment.NewLine);
				});
			}	
		}



		// for now if the same user keep connecting, do nothing. Might need to change in the future
		public void ProcessIncomingData(Socket client){			
			Package deliveryPackage;

			int buffer = 1024;
			int bytesRead = 0;
			byte[] data = new byte[buffer];

			using(MemoryStream ms = new MemoryStream()){
				while( (bytesRead = client.Receive(data, 0, buffer, SocketFlags.None)) > 0)
					ms.Write(data, 0, bytesRead);

				ms.Seek(0, SeekOrigin.Begin);

				BinaryFormatter bf = new BinaryFormatter();
				deliveryPackage = (Package)bf.Deserialize(ms);
			}			

			switch(deliveryPackage.PackageStatus){
				case PackageStatus.Connect:
					if(friendsProfileDict.ContainsKey(deliveryPackage.PublicProfile.GlobalId))
						return;

					// get the remote IPEndPoint
					IPEndPoint remoteAdd = new IPEndPoint( ((IPEndPoint)client.RemoteEndPoint).Address, deliveryPackage.Port );
					friendsIPaddressDict.TryAdd(deliveryPackage.PublicProfile.GlobalId, remoteAdd);

					friendsProfileDict.TryAdd(deliveryPackage.PublicProfile.GlobalId, deliveryPackage.PublicProfile);

					Package replyPackage = new Package(userPublicProfile, null, PackageStatus.Connect, null, defaultPort);
					using(MemoryStream ms = new MemoryStream()){
						BinaryFormatter bf = new BinaryFormatter();
						bf.Serialize(ms, replyPackage);
						ms.Seek(0, SeekOrigin.Begin);
						byte[] raw = ms.ToArray();
						
						Socket replySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
						replySocket.Connect(remoteAdd);
						replySocket.Send(raw, 0, raw.Length, SocketFlags.None);
						replySocket.Close();
						replySocket = null;
					}
					break;

				case PackageStatus.Message:
					arrivedPackage = deliveryPackage;
					hasPackage = true;
					break;

				case PackageStatus.NickUpdate:
					friendsProfileDict[deliveryPackage.Information.Item1].UserNick = deliveryPackage.Information.Item2;
					break;

				case PackageStatus.LogOff:
					IPEndPoint dummyEndPoint;
					IPublicProfile dummyProfile;
					friendsIPaddressDict.TryRemove(deliveryPackage.Information.Item1, out dummyEndPoint);
					friendsProfileDict.TryRemove(deliveryPackage.Information.Item1, out dummyProfile);
					break;			
			}
	
			P2CDS(arrivedPackage, null);					// let subscriber know they have a package
		}	


		// if user enter wrong ip , do nothing. Maybe IP error correction should be done at the main program?
		// if user enter the wrong IP return. need to implement a better way to let user know ip address is wrong
		public void ConnectToRemote(string ip){
			string[] ipParts = ip.Split(new string[]{":"}, StringSplitOptions.None);

			IPAddress remoteIP;

			if(!IPAddress.TryParse(ipParts[0], out remoteIP))
				return;

			int port;

			if(!int.TryParse(ipParts[1], out port))
				return;
			else if(port < 1 || port > ushort.MaxValue)
				return;

			DeliverConnectRequest(new IPEndPoint(remoteIP, port));	
		}

		public void DeliverConnectRequest(IPEndPoint remoteEndPoint){
			Package deliveryPackage = new Package(userPublicProfile, null, PackageStatus.Connect, null, defaultPort);
			
			try{
				using(MemoryStream ms = new MemoryStream()){
					BinaryFormatter bf = new BinaryFormatter();

					bf.Serialize(ms, deliveryPackage);
					ms.Seek(0, SeekOrigin.Begin);

					byte[] data = ms.ToArray();

					Socket remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					remoteSocket.Connect(remoteEndPoint);
					remoteSocket.Send(data, 0, data.Length, SocketFlags.None);
					remoteSocket.Close();
					remoteSocket = null;
				}
			}
			catch(Exception ex){
				Task.Factory.StartNew(()=>{
					MessageBox.Show("Inside DeliverConnectRequest." + Environment.NewLine +
									"Remote End Point: " + remoteEndPoint.ToString() + Environment.NewLine +
									"Exception Type: " + ex.GetType() + Environment.NewLine +
									"Message: " + ex.Message + Environment.NewLine);
				});
			}
		}

		#endregion Methods

	}
}
