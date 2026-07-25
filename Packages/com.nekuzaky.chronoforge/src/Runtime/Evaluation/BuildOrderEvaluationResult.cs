using System;
using System.Collections.Generic;

namespace Chronoforge
{
    /// <summary>
    /// Combined output of a simulate-and-validate pass: the computed timeline plus every
    /// issue found. Purely descriptive — callers decide how to surface it.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderEvaluationResult
    {
        public List<BuildOrderTimelineEntry> m_Timeline = new();
        public List<BuildOrderValidationIssue> m_Issues = new();
        public List<BuildOrderBenchmarkResult> m_BenchmarkResults = new();
        public float m_TotalSeconds;
        public int m_FinalSupply;

        /// <summary>
        /// Looks up the outcome of the benchmark at <paramref name="benchmarkIndex"/>. Callers use
        /// this instead of scanning the list themselves, so "no result yet" and "failed" stay
        /// distinguishable.
        /// </summary>
        public bool TryGetBenchmarkResult(int benchmarkIndex, out BuildOrderBenchmarkResult result)
        {
            for (int i = 0; i < m_BenchmarkResults.Count; i++)
            {
                if (m_BenchmarkResults[i].m_BenchmarkIndex == benchmarkIndex)
                {
                    result = m_BenchmarkResults[i];
                    return true;
                }
            }
            result = null;
            return false;
        }

        public int ErrorCount => CountBySeverity(BuildOrderIssueSeverity.Error);
        public int WarningCount => CountBySeverity(BuildOrderIssueSeverity.Warning);
        public bool IsValid => ErrorCount == 0;

        private int CountBySeverity(BuildOrderIssueSeverity severity)
        {
            int count = 0;
            foreach (BuildOrderValidationIssue issue in m_Issues)
            {
                if (issue.m_Severity == severity)
                    count++;
            }
            return count;
        }
    }
}
