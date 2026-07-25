using NUnit.Framework;
using static Chronoforge.Tests.BuildOrderTestFactory;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderSimulationTests
    {
        [Test]
        public void Simulator_AccumulatesSupplyFromDeltas()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("a", "Overlord", time: 0, delta: 8));
            asset.m_Steps.Add(Step("b", "Overlord", time: 30, delta: 8));

            BuildOrderEvaluationResult result = BuildOrderSimulator.Evaluate(asset);

            Assert.AreEqual(2, result.m_Timeline.Count);
            Assert.AreEqual(16, result.m_FinalSupply);
        }

        [Test]
        public void Benchmark_BelowTarget_FailsAndReportsMiss()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("a", "Worker", time: 0, delta: 5));
            asset.m_Benchmarks.Add(new BuildOrderBenchmark
            {
                m_Label = "Too ambitious",
                m_AnchorTimeSeconds = 10f,
                m_CheckSupply = true,
                m_ExpectedSupply = 100
            });

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsTrue(HasCode(result.m_Issues, BuildOrderValidationCode.BenchmarkMissed));
            Assert.AreEqual(1, result.m_BenchmarkResults.Count);
            Assert.IsFalse(result.m_BenchmarkResults[0].m_Passed);
        }

        [Test]
        public void Benchmark_MetTarget_Passes()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("a", "Worker", time: 0, delta: 5));
            asset.m_Benchmarks.Add(new BuildOrderBenchmark
            {
                m_AnchorTimeSeconds = 10f,
                m_CheckSupply = true,
                m_ExpectedSupply = 3
            });

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.AreEqual(1, result.m_BenchmarkResults.Count);
            Assert.IsTrue(result.m_BenchmarkResults[0].m_Passed);
        }
    }
}
