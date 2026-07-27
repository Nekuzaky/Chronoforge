using System;

namespace Chronoforge
{
    /// <summary>
    /// A target checkpoint the build order should hit, e.g. "Factory by 2:24 at 21 supply".
    /// Benchmarks are how competitive build orders express intent beyond the raw sequence; the
    /// evaluator compares them against the simulated state and flags slippage. Kept generic:
    /// checks projected supply at a time, and optionally that a specific step completes in time.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderBenchmark
    {
        public string m_Label = "";
        public float m_AnchorTimeSeconds;

        public bool m_CheckSupply;
        public int m_ExpectedSupply;

        /// <summary>Optional: id of a step that must be finished by the anchor (± tolerance).</summary>
        public string m_RequiredStepId = "";
        public float m_ToleranceSeconds = 5f;

        public string DisplayAnchor => BuildOrderTime.Format(m_AnchorTimeSeconds);
    }
}
