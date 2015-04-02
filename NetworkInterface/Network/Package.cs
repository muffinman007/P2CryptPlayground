#region Header

/*
 * The delivery package. Signature required.
 * This class is the class that will be seralized before it get sent through the net.
 *
 * Can use properties if  you want, can change it around if like you but it must contain a PublicProfile and a byte[]
 *
 */

#endregion Header

using System;


namespace NetworkInterface 
{
	[Serializable]
	public class Package
	{
		#region Fields

		public byte[] data;
		public string user;				// change string to PublicProfile

		#endregion Fields

		#region Constructors

		public Package(string user, byte[] data)
		{
			this.user = user;
			this.data = data;
		}

		#endregion Constructors
	}
}
