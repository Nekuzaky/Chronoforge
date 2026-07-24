using System.Collections.Generic;

namespace Chronoforge
{
    /// <summary>
    /// Compares an asset's benchmarks against a simulated timeline and reports slippage.
    /// Stateless and generic: it checks projected supply at a checkpoint and, optionally, that a
    /// named step finishes by the checkpoint. No game-specific metric is hardcoded.
    /// </summary>
    public static class BuildOrderBenchmarkEvaluator
    {
        public static List<BuildOrderValidationIssue> Evaluate(BuildOrderAsset asset, BuildOrderEvaluationResult simulation)
        {
            var issues = new List<BuildOrderValidationIssue>();
            if (asset == null || simulation == null)
                return issues;

            foreach (BuildOrderBenchmark benchmark in asset.m_Benchmarks)
            {
                CheckSupply(benchmark, simulation, issues);
                CheckRequiredStep(asset, benchmark, simulation, issues);
            }

            return issues;
        }

        private static void CheckSupply(
            BuildOrderBenchmark benchmark, BuildOrderEvaluationResult simulation, List<BuildOrderValidationIssue> issues)
        {
            if (!benchmark.m_CheckSupply)
                return;

            int projected = ProjectedSupplyAt(simulation, benchmark.m_AnchorTimeSeconds);
            if (projected < benchmark.m_ExpectedSupply)
            {
                issues.Add(new BuildOrderValidationIssue(
                    BuildOrderIssueSeverity.Warning,
                    BuildOrderValidationCode.BenchmarkMissed,
                    $"Benchmark '{Name(benchmark)}': supply {projected} at {benchmark.DisplayAnchor}, expected {benchmark.m_ExpectedSupply}."));
            }
        }

        private static void CheckRequiredStep(
            BuildOrderAsset asset,
            BuildOrderBenchmark benchmark,
            BuildOrderEvaluationResult simulation,
            List<BuildOrderValidationIssue> issues)
        {
            if (string.IsNullOrEmpty(benchmark.m_RequiredStepId))
                return;

            BuildOrderTimelineEntry entry = simulation.m_Timeline.Find(e => e.m_StepId == benchmark.m_RequiredStepId);
            if (entry == null)
            {
                issues.Add(new BuildOrderValidationIssue(
                    BuildOrderIssueSeverity.Warning,
                    BuildOrderValidationCode.BenchmarkMissed,
                    $"Benchmark '{Name(benchmark)}': required step is missing from the build."));
                return;
            }

            float deadline = benchmark.m_AnchorTimeSeconds + benchmark.m_ToleranceSeconds;
            if (entry.m_EndSeconds > deadline)
            {
                BuildOrderStep step = asset.FindStep(benchmark.m_RequiredStepId);
                string stepName = step != null && !string.IsNullOrEmpty(step.m_Title) ? step.m_Title : benchmark.m_RequiredStepId;
                issues.Add(new BuildOrderValidationIssue(
                    BuildOrderIssueSeverity.Warning,
                    BuildOrderValidationCode.BenchmarkMissed,
                    $"Benchmark '{Name(benchmark)}': '{stepName}' finishes at {BuildOrderTime.Format(entry.m_EndSeconds)}, target {benchmark.DisplayAnchor}.",
                    benchmark.m_RequiredStepId,
                    entry.m_StepIndex));
            }
        }

        /// <summary>Supply after the last step that has started by <paramref name="time"/>.</summary>
        private static int ProjectedSupplyAt(BuildOrderEvaluationResult simulation, float time)
        {
            int supply = 0;
            foreach (BuildOrderTimelineEntry entry in simulation.m_Timeline)
            {
                if (entry.m_StartSeconds <= time)
                    supply = entry.m_ProjectedSupply;
                else
                    break;
            }
            return supply;
        }

        private static string Name(BuildOrderBenchmark benchmark) =>
            string.IsNullOrEmpty(benchmark.m_Label) ? benchmark.DisplayAnchor : benchmark.m_Label;
    }
}
