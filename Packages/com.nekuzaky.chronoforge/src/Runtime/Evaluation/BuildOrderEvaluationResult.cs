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
        public float m_TotalSeconds;
        public int m_FinalSupply;

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
