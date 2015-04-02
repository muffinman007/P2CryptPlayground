﻿#region Header

/**
 * 
 * Controls outgoing/incoming data for the program.
 * 
 **/

#endregion Header

using System;


namespace Network 
{
	public class NetworkServer 
	{

		#region Fields
		//switch
		bool hasPackage;							// when NetworkServer received data and finish de-serializing it this turn to true
		bool hasServerStart;

		// Socket data
		int port;
		int backlog;

		Package package;

		#endregion Fields


		#region Constructors

		public NetworkServer(int port = 12345, int backlog = 100)
		{
			this.port = port;
			this.backlog = backlog;
			hasPackage = false;
			hasServerStart = false;
		}

		#endregion Constructors


		#region Properties


		/// <summary>
		/// Allow user to get the most recent arrived package.
		/// </summary>
		public Package Package
		{
			get
			{
				if(hasPackage)
					return package;
				else{
					return null;						// should we return an empty package?
				}
			}

			private set
			{
				package = value;
			}
		}

		#endregion Properties


		#region Methods

		/// <summary>
		/// Start the process in which NetworkServer listen for incoming connection. Runs on a Separate Thread.
		/// </summary>
		public void Start()
		{
			if(hasServerStart)
				return;


			hasServerStart = true;
		}


		/// <summary>
		/// Gracefully disconnect the NetworkServer
		/// </summary>
		public void Disconnect()
		{
			//// TO-DO //testchange

			hasServerStart = false;
		}

		/// <summary>
		/// Send the package to all connected node. On fail do nothing.
		/// </summary>
		/// <param name="deliveryPackage">The current data user want to send out</param>
		public void Send(Package deliveryPackage)
		{

		}


		#endregion Methods

	}
}
