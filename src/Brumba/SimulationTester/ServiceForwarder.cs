using Microsoft.Ccr.Core;

namespace Brumba.Simulation.SimulationTester
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
    }
}