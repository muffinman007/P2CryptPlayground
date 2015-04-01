using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkInterface 
{
	public class Network {
		#region Fields
		//switch
		bool hasPackage;							// when network received data and finish de-serializing it this turn to true

		// important data
		int port;
		#endregion Fields


		#region Constructors

		public Network(int port = 12345)
		{
			this.port = port;
			hasPackage = false;
		}

		#endregion Constructors


		#region Properties
		public Package Package
		{
			if(

		#endregion Properties


		#region Methods


		#endregion Methods
	}
}
