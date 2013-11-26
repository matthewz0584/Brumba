using Microsoft.Ccr.Core;

namespace Brumba.SimulationTester
{
    //Wrapper for SimulationTesterService.ForwardTo method to remove direct dependency
    public class ServiceForwarder
    {
        private SimulationTesterService _sts;

        public ServiceForwarder(SimulationTesterService sts)
        {
            _sts = sts;
        }

        public T ForwardTo<T>(string serviceUri) where T : IPortSet, new()
        {
            return _sts.ForwardTo<T>(serviceUri);
        }

        public void Activate<T>(params T[] tasks) where T : ITask
        {
            _sts.Activate(tasks);
        }
    }
}