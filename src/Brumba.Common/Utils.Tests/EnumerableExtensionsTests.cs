using NUnit.Framework;

namespace Brumba.Utils.Tests
{
    [TestFixture]
    public class EnumerableExtensionsTests
    {
        [Test]
        public void FilterSequencialDuplicates()
        {
            Assert.That(new int[] {}.FilterSequencialDuplicates(), Is.Empty);
            Assert.That(new[] { 1, 2 }.FilterSequencialDuplicates(), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(new[] { 1, 1 }.FilterSequencialDuplicates(), Is.EquivalentTo(new[] { 1 }));
            Assert.That(new[] { 1, 1, 2, 2 }.FilterSequencialDuplicates(), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(new[] { 1, 2, 1, 2 }.FilterSequencialDuplicates(), Is.EquivalentTo(new[] { 1, 2, 1, 2 }));
        }
    }
}