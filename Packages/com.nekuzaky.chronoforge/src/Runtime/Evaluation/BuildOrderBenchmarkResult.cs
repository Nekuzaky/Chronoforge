using System;

namespace Chronoforge
{
    /// <summary>
    /// Per-benchmark outcome of an evaluation, keyed by the benchmark's index in
    /// <see cref="BuildOrderAsset.m_Benchmarks"/>. Lets the editor show a live pass/fail state
    /// per checkpoint without re-running the analysis or string-matching issue messages.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderBenchmarkResult
    {
        public int m_BenchmarkIndex = -1;
        public bool m_Passed = true;
        public string m_Detail = "";

        public BuildOrderBenchmarkResult() { }

        public BuildOrderBenchmarkResult(int benchmarkIndex, bool passed, string detail)
        {
            m_BenchmarkIndex = benchmarkIndex;
            m_Passed = passed;
            m_Detail = detail;
        }
    }
}
