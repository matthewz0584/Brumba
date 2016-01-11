using Brumba.GenericTimer.Proxy;
using Microsoft.Ccr.Core;

namespace Brumba.SimulationTester
{
    public static class SimulatedTimerUtils
    {
        public static Subscribe Subscribe(this TimerOperations me, float interval)
        {
            var subscribeRq = new Subscribe
                {
                    Body = new SubscribeRequest(interval),
                    NotificationPort = new TimerOperations(),
                    NotificationShutdownPort = new Port<Shutdown>()
                };
            me.Post(subscribeRq);
            return subscribeRq;
        }
    }
}