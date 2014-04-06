using Microsoft.Ccr.Core;

namespace Brumba.SimulationTester
{
    public static class SimulatedTimerUtils
    {
        public static Entities.Timer.Proxy.Subscribe Subscribe(this Entities.Timer.Proxy.TimerOperations me, float interval)
        {
            var subscribeRq = new Entities.Timer.Proxy.Subscribe
                {
                    Body = new Entities.Timer.Proxy.SubscribeRequest(interval),
                    NotificationPort = new Entities.Timer.Proxy.TimerOperations(),
                    NotificationShutdownPort = new Port<Shutdown>()
                };
            me.Post(subscribeRq);
            return subscribeRq;
        }
    }
}