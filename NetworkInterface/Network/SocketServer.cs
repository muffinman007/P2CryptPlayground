using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using System.Net;

namespace Network {
	protected internal class SocketServer {

		#region Fields

		Socket server;
		int backlog;

		#endregion Fields


		#region Constructors

		public SocketServer(int port, int backlog){
			this.backlog = backlog;

			IPEndPoint endPoint = new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], port);
			server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			server.Bind(endPoint);
			server.LingerState = new LingerOption(false, 0);
		}

		#endregion Constructors


		#region Properties
		#endregion Properties


		#region Methods
		#endregion Methods

	}
}
