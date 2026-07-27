using NUnit.Framework;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderTimeTests
    {
        [Test]
        public void Format_BuildsMinutesAndZeroPaddedSeconds()
        {
            Assert.AreEqual("1:05", BuildOrderTime.Format(65f));
            Assert.AreEqual("0:00", BuildOrderTime.Format(0f));
            Assert.AreEqual("0:00", BuildOrderTime.Format(-10f));
        }

        [Test]
        public void TryParse_AcceptsMinutesSeconds()
        {
            Assert.IsTrue(BuildOrderTime.TryParse("1:30", out float seconds));
            Assert.AreEqual(90f, seconds);
        }

        [Test]
        public void TryParse_AcceptsPlainSeconds()
        {
            Assert.IsTrue(BuildOrderTime.TryParse("90", out float seconds));
            Assert.AreEqual(90f, seconds);
        }

        [Test]
        public void TryParse_RejectsGarbage()
        {
            Assert.IsFalse(BuildOrderTime.TryParse("abc", out _));
            Assert.IsFalse(BuildOrderTime.TryParse("", out _));
        }
    }
}
