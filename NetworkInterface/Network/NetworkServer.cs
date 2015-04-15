#region Header

/**
 * 
 * Controls outgoing/incoming data for the program.
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
		
		ConcurrentDictionary<string, IPublicProfile> buddyConcurrentDict;
		ConcurrentDictionary<string, Socket>		clientSocketDict;

		Socket server;

		CancellationTokenSource tokenSource;
		Task serverTask;
		Task[] sendTask;

		Package arrivedPackage;

		string userNick;

		#endregion Fields


		#region Constructors

		public NetworkServer(string userNick, int port = 12345, int backlog = 100)
		{	
			this.userNick = userNick;

			hasPackage = false;
		
			buddyConcurrentDict = new ConcurrentDictionary<string,IPublicProfile>();
			clientSocketDict	= new ConcurrentDictionary<string,Socket>();

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
		}


		/// <summary>
		/// Gracefully disconnect the NetworkServer
		/// </summary>
		public void Disconnect()
		{
			if(serverTask == null)
				return;

			//server.Shutdown(SocketShutdown.Both);
			server.Close();
			//server = null;

			tokenSource.Cancel();

			serverTask = null;
		}

		/// <summary>
		/// Send the package to all connected node. On fail do nothing.
		/// </summary>
		/// <param name="PendingPackageState">The package user is sending out</param>
		public async void Send(PackageStatus status, string strData)
		{
			// There's a better way to do this so we can catch all the socketException and handle it correctly.
			// When we catch an exception most likely user had disconnected and we need to update that change
			// with the program.			
			Socket client = null;	
			byte[] outgoingData = null;

			try{									
				foreach(var outgoingSocket in clientSocketDict){
					using(MemoryStream ms = new MemoryStream()){            // might need to handle memory error in the future
						BinaryFormatter bf = new BinaryFormatter();
						client = outgoingSocket.Value;

						Package deliveryPackage = null;

						if(status == PackageStatus.Message){
							IPublicProfile outgoingProfile;
							buddyConcurrentDict.TryGetValue(outgoingSocket.Key, out outgoingProfile);

							deliveryPackage = new Package(
									null, 
									userNick,
									PackageStatus.Message,
									outgoingProfile.Encrypt(Encoding.UTF8.GetBytes(strData))
								);
						}
						else{

						}

						bf.Serialize(ms, deliveryPackage);
						ms.Seek(0, SeekOrigin.Begin);
						outgoingData = ms.ToArray();
						outgoingSocket.Value.Send(outgoingData, 0, outgoingData.Length, SocketFlags.None);
					}
				}
			}
			catch(SocketException se){
				Task.Factory.StartNew(()=>{
					MessageBox.Show("Error while sending data" + Environment.NewLine +
									"Remote ip: " + client.RemoteEndPoint.ToString() + Environment.NewLine +
									"Socket Exception: " + Environment.NewLine +
									se.Message + Environment.NewLine +
									"Error Code: " + se.NativeErrorCode + Environment.NewLine);
				});
			}
			catch(Exception ex){
				Task.Factory.StartNew(()=>{
					MessageBox.Show("Error while sending data" + Environment.NewLine +
									ex.Message + Environment.NewLine);
				});
			}			
		}


		void ProcessIncomingData(Socket client){
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
				case PackageStatus.SignIn:
					break;

				case PackageStatus.LogOff:
					break;

				case PackageStatus.NickUpdate:
					break;

				case PackageStatus.Message:
					arrivedPackage = deliveryPackage;
					hasPackage = true;
					P2CDS(arrivedPackage, null);					// let subscriber know they have a package
					break;
			}

			clientSocketDict.TryAdd(deliveryPackage.PublicProfile.UserNick, client);
			buddyConcurrentDict.TryAdd(deliveryPackage.PublicProfile.UserNick, deliveryPackage.PublicProfile);
		}


		#endregion Methods

	}
}
