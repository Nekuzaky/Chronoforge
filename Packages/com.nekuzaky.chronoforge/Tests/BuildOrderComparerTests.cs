using NUnit.Framework;
using static Chronoforge.Tests.BuildOrderTestFactory;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderComparerTests
    {
        [Test]
        public void Compare_DetectsAddedRemovedAndTimeShift()
        {
            BuildOrderAsset baseline = Asset();
            baseline.m_Steps.Add(Step("a", "Pool", time: 0));
            baseline.m_Steps.Add(Step("b", "Expand", time: 60));

            BuildOrderAsset current = Asset();
            current.m_Steps.Add(Step("a", "Pool", time: 30));   // time shift
            current.m_Steps.Add(Step("c", "Rush", time: 90));   // added

            BuildOrderDiff diff = BuildOrderComparer.Compare(baseline, current);

            Assert.AreEqual(1, diff.m_Added);
            Assert.AreEqual(1, diff.m_Removed);
            Assert.AreEqual(1, diff.m_Changed);
            Assert.IsTrue(diff.HasChanges);
        }

        [Test]
        public void Compare_IdenticalBuild_HasNoChanges()
        {
            BuildOrderAsset baseline = Asset();
            baseline.m_Steps.Add(Step("a", "Pool", time: 0));

            BuildOrderAsset current = Asset();
            current.m_Steps.Add(Step("a", "Pool", time: 0));

            BuildOrderDiff diff = BuildOrderComparer.Compare(baseline, current);

            Assert.IsFalse(diff.HasChanges);
        }
    }
}
