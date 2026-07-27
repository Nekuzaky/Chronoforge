using System;
using System.Collections.Generic;

namespace Chronoforge
{
    /// <summary>How a step differs between a baseline (planned) and the current (actual) build.</summary>
    public enum BuildOrderChangeType
    {
        Unchanged,
        Added,
        Removed,
        TimeShift,
        SupplyShift,
        Retyped
    }

    /// <summary>One row of a planned-vs-actual comparison, keyed by step id.</summary>
    [Serializable]
    public sealed class BuildOrderDiffEntry
    {
        public string m_StepId = "";
        public string m_Title = "";
        public BuildOrderChangeType m_ChangeType = BuildOrderChangeType.Unchanged;
        public string m_Detail = "";
        public float m_DeltaSeconds;
    }

    /// <summary>Aggregate result of a comparison.</summary>
    [Serializable]
    public sealed class BuildOrderDiff
    {
        public List<BuildOrderDiffEntry> m_Entries = new();

        public int m_Added;
        public int m_Removed;
        public int m_Changed;

        public bool HasChanges => m_Added > 0 || m_Removed > 0 || m_Changed > 0;
    }
}
