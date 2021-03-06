using System;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using DC = System.Diagnostics.Contracts;

namespace Brumba.Common
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

		public new void SpawnIterator(IteratorHandler handler)
		{
			base.SpawnIterator(handler);
		}

        //[SuppressMessage("Microsoft.Contracts", "Ensures", Justification = "base.ServiceForwarder has no contract")]
		public new T ServiceForwarder<T>(Uri uri) where T : IPort, IPortSet, new()
		{
            DC.Contract.Requires(uri != null);
            DC.Contract.Ensures(DC.Contract.Result<T>() != null);

			return base.ServiceForwarder<T>(uri);
		}

        public new T ServiceForwarder<T>(string uri) where T : IPort, IPortSet, new()
		{
            DC.Contract.Requires(!string.IsNullOrEmpty(uri));
            DC.Contract.Ensures(DC.Contract.Result<T>() != null);

			return base.ServiceForwarder<T>(uri);
		}

		public new DsspResponsePort<ServiceInfoType> DirectoryQuery(string contract, TimeSpan expiration)
		{
            DC.Contract.Requires(!string.IsNullOrEmpty(contract));
            DC.Contract.Ensures(DC.Contract.Result<DsspResponsePort<ServiceInfoType>>() != null);

			return base.DirectoryQuery(contract, expiration);
		}

		public new Port<DateTime> TimeoutPort(int milliseconds)
		{
            DC.Contract.Requires(milliseconds >= 0);
            DC.Contract.Ensures(DC.Contract.Result<Port<DateTime>>() != null);

			return base.TimeoutPort(milliseconds);
		}

		public new DispatcherQueue TaskQueue
		{
			get { return base.TaskQueue; }
		}

		public new void LogInfo(string text)
		{
			DC.Contract.Requires(text != null);

			base.LogInfo(text);
		}

		public void LogInfo(string text, params object[] args)
		{
            DC.Contract.Requires(text != null);
            DC.Contract.Requires(args != null);

			base.LogInfo(string.Format(text, args));
		}

        public new void LogInfo(Enum category, params object[] args)
        {
            DC.Contract.Requires(args != null);

            base.LogInfo(category, args);
        }
	}
}