using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace Network {
	class SocketServer{

		#region Fields

		Socket server;
		Thread runThread;

		#endregion Fields


		#region Constructors

		public SocketServer(int port, int backlog){
			IPEndPoint endPoint = new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], port);
			server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			server.Bind(endPoint);
			server.LingerState = new LingerOption(false, 0);
			server.Listen(backlog);
		}

		#endregion Constructors


		#region Properties
		#endregion Properties


		#region Methods

		public void Run(){
			runThread = new Thread(()=>{
				try{
					while(true){
						Socket client = server.Accept();

					}
				}
				catch(ThreadAbortException){

				}
			});
		}

		#endregion Methods

	}
}
