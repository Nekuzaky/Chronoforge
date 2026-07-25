using NUnit.Framework;
using static Chronoforge.Tests.BuildOrderTestFactory;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderTimelineQueryTests
    {
        private static BuildOrderEvaluationResult ThreeStepBuild()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("a", "First", time: 0, delta: 2));
            asset.m_Steps.Add(Step("b", "Second", time: 30, delta: 3));
            asset.m_Steps.Add(Step("c", "Third", time: 60, delta: 4));
            return BuildOrderSimulator.Evaluate(asset);
        }

        [Test]
        public void IndexAt_BeforeFirstStep_IsMinusOne()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("a", "Late", time: 10));

            Assert.AreEqual(-1, BuildOrderTimelineQuery.IndexAt(BuildOrderSimulator.Evaluate(asset), 5f));
        }

        [Test]
        public void IndexAt_ReturnsLastStartedStep()
        {
            BuildOrderEvaluationResult result = ThreeStepBuild();

            Assert.AreEqual(0, BuildOrderTimelineQuery.IndexAt(result, 0f));
            Assert.AreEqual(0, BuildOrderTimelineQuery.IndexAt(result, 29f));
            Assert.AreEqual(1, BuildOrderTimelineQuery.IndexAt(result, 30f));
            Assert.AreEqual(2, BuildOrderTimelineQuery.IndexAt(result, 999f));
        }

        [Test]
        public void IndexAt_NullResult_IsMinusOne() =>
            Assert.AreEqual(-1, BuildOrderTimelineQuery.IndexAt(null, 10f));

        [Test]
        public void SupplyAt_TracksCumulativeSupply()
        {
            BuildOrderEvaluationResult result = ThreeStepBuild();

            Assert.AreEqual(0, BuildOrderTimelineQuery.SupplyAt(result, -1f));
            Assert.AreEqual(2, BuildOrderTimelineQuery.SupplyAt(result, 0f));
            Assert.AreEqual(5, BuildOrderTimelineQuery.SupplyAt(result, 30f));
            Assert.AreEqual(9, BuildOrderTimelineQuery.SupplyAt(result, 60f));
        }

        [Test]
        public void TryGetBenchmarkResult_DistinguishesMissingFromFailed()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("a", "Worker", time: 0, delta: 1));
            asset.m_Benchmarks.Add(new BuildOrderBenchmark
            {
                m_AnchorTimeSeconds = 10f,
                m_CheckSupply = true,
                m_ExpectedSupply = 50
            });

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsTrue(result.TryGetBenchmarkResult(0, out BuildOrderBenchmarkResult first));
            Assert.IsFalse(first.m_Passed);
            Assert.IsFalse(result.TryGetBenchmarkResult(7, out _), "absent benchmark must report as missing");
        }
    }
}
