using System;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;

namespace Brumba.Utils
{
	public class DsspServiceExposing : DsspServiceBase
	{
		public DsspServiceExposing(ServiceEnvironment environment) : base(environment)
		{
		}

		public DsspServiceExposing(DsspServiceCreationPort creationPort) : base(creationPort)
		{
		}

		public DsspServiceExposing(DsspServiceBase source) : base(source)
		{
		}

		public T ServiceForwarder<T>(Uri uri) where T : IPort, IPortSet, new()
		{
			return base.ServiceForwarder<T>(uri);
		}

		public DsspResponsePort<ServiceInfoType> DirectoryQuery(string contract, TimeSpan expiration)
		{
			return base.DirectoryQuery(contract, expiration);
		}

		public Port<DateTime> TimeoutPort(int milliseconds)
		{
			return base.TimeoutPort(milliseconds);
		}

		public DispatcherQueue TaskQueue
		{
			get { return base.TaskQueue; }
		}
	}
}