﻿#region Header

/*
 * The delivery package. Signature required.
 * This class is the class that will be seralized before it get sent through the net.
 */

#endregion Header

using System;
using P2CCommon;

namespace Network
{
    [Serializable]
    public class Package : IPackage {

		#region Fields

		IPublicProfile publicProfile;
		byte[] data;
		PackageStatus status;

		#endregion Fields


		#region Properties

		public IPublicProfile PublicProfile 
		{
			get{ return publicProfile; }
		}

		public byte[] Data 
		{
			get { return data; }
		}

		public PackageStatus PackageStatus
		{
			get{ return status; }
		}
		
        #endregion Properties

        #region Constructors

        public Package(IPublicProfile userProfile, PackageStatus status, byte[] data)
        {
            this.publicProfile = userProfile;
			this.status = status;
            this.data = data;
        }

        #endregion Constructors
		
	}
}