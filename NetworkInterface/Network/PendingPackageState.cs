using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using P2CCommon;

namespace Network 
{
	public class PendingPackageState<T> : IPendingPackageState<T>
	{
		public T Data { get; set; }

		public PackageStatus PackageStatus { get; set; }

		public PendingPackageState(T data, PackageStatus status)
		{
			Data = data;
			PackageStatus = status;
		}
	}
}
