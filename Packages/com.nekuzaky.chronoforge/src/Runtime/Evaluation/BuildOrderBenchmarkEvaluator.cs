using System.Collections.Generic;

namespace Chronoforge
{
    /// <summary>
    /// Compares an asset's benchmarks against a simulated timeline. Writes both a per-benchmark
    /// pass/fail result and, on failure, a validation issue into the supplied result. Stateless
    /// and generic: it checks projected supply at a checkpoint and, optionally, that a named step
    /// finishes by the checkpoint. No game-specific metric is hardcoded.
    /// </summary>
    public static class BuildOrderBenchmarkEvaluator
    {
        public static void Evaluate(BuildOrderAsset asset, BuildOrderEvaluationResult result)
        {
            if (asset == null || result == null)
                return;

            for (int index = 0; index < asset.m_Benchmarks.Count; index++)
            {
                BuildOrderBenchmark benchmark = asset.m_Benchmarks[index];
                var details = new List<string>();
                bool passed = true;

                passed &= CheckSupply(benchmark, result, index, details);
                passed &= CheckRequiredStep(asset, benchmark, result, index, details);

                result.m_BenchmarkResults.Add(new BuildOrderBenchmarkResult(index, passed, string.Join("  ·  ", details)));
            }
        }

        private static bool CheckSupply(
            BuildOrderBenchmark benchmark, BuildOrderEvaluationResult result, int index, List<string> details)
        {
            if (!benchmark.m_CheckSupply)
                return true;

            int projected = ProjectedSupplyAt(result, benchmark.m_AnchorTimeSeconds);
            bool ok = projected >= benchmark.m_ExpectedSupply;
            details.Add($"supply {projected}/{benchmark.m_ExpectedSupply}");

            if (!ok)
            {
                result.m_Issues.Add(new BuildOrderValidationIssue(
                    BuildOrderIssueSeverity.Warning,
                    BuildOrderValidationCode.BenchmarkMissed,
                    $"Benchmark '{Name(benchmark)}': supply {projected} at {benchmark.DisplayAnchor}, expected {benchmark.m_ExpectedSupply}."));
            }
            return ok;
        }

        private static bool CheckRequiredStep(
            BuildOrderAsset asset,
            BuildOrderBenchmark benchmark,
            BuildOrderEvaluationResult result,
            int index,
            List<string> details)
        {
            if (string.IsNullOrEmpty(benchmark.m_RequiredStepId))
                return true;

            BuildOrderTimelineEntry entry = result.m_Timeline.Find(e => e.m_StepId == benchmark.m_RequiredStepId);
            if (entry == null)
            {
                details.Add("required step missing");
                result.m_Issues.Add(new BuildOrderValidationIssue(
                    BuildOrderIssueSeverity.Warning,
                    BuildOrderValidationCode.BenchmarkMissed,
                    $"Benchmark '{Name(benchmark)}': required step is missing from the build."));
                return false;
            }

            float deadline = benchmark.m_AnchorTimeSeconds + benchmark.m_ToleranceSeconds;
            bool ok = entry.m_EndSeconds <= deadline;
            details.Add($"done {BuildOrderTime.Format(entry.m_EndSeconds)}");

            if (!ok)
            {
                BuildOrderStep step = asset.FindStep(benchmark.m_RequiredStepId);
                string stepName = step != null && !string.IsNullOrEmpty(step.m_Title) ? step.m_Title : benchmark.m_RequiredStepId;
                result.m_Issues.Add(new BuildOrderValidationIssue(
                    BuildOrderIssueSeverity.Warning,
                    BuildOrderValidationCode.BenchmarkMissed,
                    $"Benchmark '{Name(benchmark)}': '{stepName}' finishes at {BuildOrderTime.Format(entry.m_EndSeconds)}, target {benchmark.DisplayAnchor}.",
                    benchmark.m_RequiredStepId,
                    entry.m_StepIndex));
            }
            return ok;
        }

        /// <summary>Supply after the last step that has started by <paramref name="time"/>.</summary>
        private static int ProjectedSupplyAt(BuildOrderEvaluationResult result, float time)
        {
            int supply = 0;
            foreach (BuildOrderTimelineEntry entry in result.m_Timeline)
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
