using System;

namespace Chronoforge
{
    /// <summary>
    /// A named variation lane. Steps join a branch through their <c>m_BranchKey</c>; the
    /// branch itself only carries presentation and its gating condition.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderBranch
    {
        public string m_Key = "";
        public string m_Name = "";
        public string m_Description = "";
        public string m_ColorHex = "#4C9AFF";
        public BuildOrderCondition m_Condition = new();

        public bool IsMainline => string.IsNullOrEmpty(m_Key);
    }
}
