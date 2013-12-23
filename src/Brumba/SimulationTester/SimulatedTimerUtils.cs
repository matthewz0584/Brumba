using Microsoft.Ccr.Core;

namespace Brumba.SimulationTester
{
    public static class SimulatedTimerUtils
    {
        public static Simulation.SimulatedTimer.Proxy.Subscribe Subscribe(this Simulation.SimulatedTimer.Proxy.SimulatedTimerOperations me, float interval)
        {
            var subscribeRq = new Simulation.SimulatedTimer.Proxy.Subscribe
                {
                    Body = new Simulation.SimulatedTimer.Proxy.SubscribeRequest(interval),
                    NotificationPort = new Simulation.SimulatedTimer.Proxy.SimulatedTimerOperations(),
                    NotificationShutdownPort = new Port<Shutdown>()
                };
            me.Post(subscribeRq);
            return subscribeRq;
        }
    }
}