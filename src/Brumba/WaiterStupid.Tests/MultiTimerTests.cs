using System;
using System.Collections.Generic;
using Brumba.Simulation.SimulatedTimer;
using NUnit.Framework;

namespace Brumba.WaiterStupid.Tests
{
    [TestFixture]
    public class MultiTimerTests
    {
        [Test]
        public void Notifications()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);

            float time = 0;
            timer.Tick += (_, t) => time = t;

            Assert.That(time, Is.EqualTo(0));

            timer.Update(0.01f);

            Assert.That(time, Is.EqualTo(0));

            timer.Update(0.09f);

            Assert.That(time, Is.EqualTo(0));

            timer.Update(0.11f);

            Assert.That(time, Is.EqualTo(0.11f));

            timer.Update(0.20f);

            Assert.That(time, Is.EqualTo(0.11f));

            timer.Update(0.211f);

            Assert.That(time, Is.EqualTo(0.211f));
        }

        [Test]
        public void NamedSubscribe()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);

            float time = 0;
            string subscriber = "";
            timer.Tick += (s, t) => { time = t; subscriber = s; };

            Assert.That(time, Is.EqualTo(0));

            timer.Update(0.01f);

            Assert.That(time, Is.EqualTo(0));

            timer.Update(0.11f);

            Assert.That(time, Is.EqualTo(0.11f));
            Assert.That(subscriber, Is.EqualTo("1"));

            timer.Subscribe("2", 0.25f);

            timer.Update(0.22f);

            Assert.That(time, Is.EqualTo(0.22f));
            Assert.That(subscriber, Is.EqualTo("1"));

            timer.Update(0.33f);

            Assert.That(time, Is.EqualTo(0.33f));
            Assert.That(subscriber, Is.EqualTo("1"));

            timer.Update(0.37f);

            Assert.That(time, Is.EqualTo(0.37f));
            Assert.That(subscriber, Is.EqualTo("2"));
        }

        [Test]
        public void NamedUnsubscribe()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);
            timer.Subscribe("2", 0.2f);

            float time = 0;
            var subscribers = new List<string>();
            timer.Tick += (s, t) => { time = t; subscribers.Add(s); };

            timer.Update(0.21f);

            Assert.That(subscribers, Is.EquivalentTo(new []{"1", "2"}));

            timer.Unsubscribe("1");
            subscribers.Clear();

            timer.Update(0.5f);

            Assert.That(subscribers, Is.EquivalentTo(new[] { "2" }));
        }

        [Test]
        [ExpectedException(typeof(Exception), ExpectedMessage = "Subscriber with name 1 is already registered")]
        public void SubscribeNonUniqueName()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);
            timer.Subscribe("1", 0.2f);
        }
    }
}