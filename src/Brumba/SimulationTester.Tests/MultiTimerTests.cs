using System;
using System.Collections.Generic;
using Brumba.Simulation.SimulatedTimer;
using NUnit.Framework;

namespace Brumba.SimulationTester.Tests
{
    [TestFixture]
    public class MultiTimerTests
    {
        [NUnit.Framework.Test]
        public void Tick()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);

            float time = 0;
	        float delta = 0;
	        timer.Tick += (_, dt, t) =>
		        {
			        time = t;
			        delta = dt;
		        };

            timer.Update(0.01f, 0.01f);

            Assert.That(time, Is.EqualTo(0));
            Assert.That(delta, Is.EqualTo(0));

            timer.Update(0.08f, 0.09f);

            Assert.That(time, Is.EqualTo(0));
            Assert.That(delta, Is.EqualTo(0));

            timer.Update(0.02f, 0.11f);

            Assert.That(time, Is.EqualTo(0.11f));
			Assert.That(delta, Is.EqualTo(0.11f));

            timer.Update(0.09f, 0.20f);

            Assert.That(time, Is.EqualTo(0.11f));
			Assert.That(delta, Is.EqualTo(0.11f));

            timer.Update(0.011f, 0.211f);

            Assert.That(time, Is.EqualTo(0.211f));
			Assert.That(delta, Is.EqualTo(0.101f).Within(1e-5));
        }

        [NUnit.Framework.Test]
        public void NamedSubscribe()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);

            var subscriber = "";
            timer.Tick += (s, _, __) => subscriber = s;

            timer.Update(0.01f, 0.01f);

            Assert.That(subscriber, Is.EqualTo(""));

            timer.Update(0.1f, 0.11f);

            Assert.That(subscriber, Is.EqualTo("1"));

            timer.Subscribe("2", 0.25f);

            timer.Update(0.11f, 0.22f);

            Assert.That(subscriber, Is.EqualTo("1"));

            timer.Update(0.11f, 0.33f);

            Assert.That(subscriber, Is.EqualTo("1"));

            timer.Update(0.04f, 0.37f);

            Assert.That(subscriber, Is.EqualTo("2"));
        }

        [NUnit.Framework.Test]
        public void NamedUnsubscribe()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);
            timer.Subscribe("2", 0.2f);

            float time = 0;
            var subscribers = new List<string>();
            timer.Tick += (s, t, _) => { time = t; subscribers.Add(s); };

            timer.Update(0.21f, 0.21f);

            Assert.That(subscribers, Is.EquivalentTo(new []{"1", "2"}));

            timer.Unsubscribe("1");
            subscribers.Clear();

            timer.Update(0.29f, 0.5f);

            Assert.That(subscribers, Is.EquivalentTo(new[] { "2" }));
        }

        [NUnit.Framework.Test]
        [ExpectedException(typeof(Exception), ExpectedMessage = "Subscriber with name 1 is already registered")]
        public void SubscribeNonUniqueName()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);
            timer.Subscribe("1", 0.2f);
        }

        [NUnit.Framework.Test]
        public void Reset()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);
            timer.Subscribe("2", 0.15f);

            timer.Update(0.21f, 0.21f);

            float delta = 0;
            var subscriber = "";
            timer.Tick += (s, dt, _) => { delta = dt; subscriber = s; };

            timer.Reset(new[] { "1" });

            Assert.That(timer.Subscribers, Is.EquivalentTo(new[] { "1" }));

            timer.Subscribe("3", 0.25f);
            timer.Update(0.05f, 0.05f);

            Assert.That(delta, Is.EqualTo(0));
            Assert.That(subscriber, Is.EqualTo(""));

            timer.Update(0.06f, 0.11f);

            Assert.That(delta, Is.EqualTo(0.11f));
            Assert.That(subscriber, Is.EqualTo("1"));

            timer.Update(0.15f, 0.26f);

            Assert.That(delta, Is.EqualTo(0.26f));
            Assert.That(subscriber, Is.EqualTo("3"));
        }

        [NUnit.Framework.Test]
        public void Subscribers()
        {
            var timer = new MultiTimer();
            timer.Subscribe("1", 0.1f);
            timer.Subscribe("2", 0.15f);

            Assert.That(timer.Subscribers, Is.EquivalentTo(new []{"1", "2"}));

            timer.Subscribe("3", 0.15f);

            Assert.That(timer.Subscribers, Is.EquivalentTo(new[] { "1", "2", "3" }));
        }
    }
}