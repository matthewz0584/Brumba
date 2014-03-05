using System;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;

namespace Brumba.DsspUtils
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

		public new T ServiceForwarder<T>(Uri uri) where T : IPort, IPortSet, new()
		{
			return base.ServiceForwarder<T>(uri);
		}

		public new T ServiceForwarder<T>(string uri) where T : IPort, IPortSet, new()
		{
			return base.ServiceForwarder<T>(uri);
		}

		public new DsspResponsePort<ServiceInfoType> DirectoryQuery(string contract, TimeSpan expiration)
		{
			return base.DirectoryQuery(contract, expiration);
		}

		public new Port<DateTime> TimeoutPort(int milliseconds)
		{
			return base.TimeoutPort(milliseconds);
		}

		public new DispatcherQueue TaskQueue
		{
			get { return base.TaskQueue; }
		}

		public void LogInfo(string text, params object[] args)
		{
			base.LogInfo(string.Format(text, args));
		}

        public new void LogInfo(Enum category, params object[] args)
        {
            base.LogInfo(category, args);
        }
	}
}