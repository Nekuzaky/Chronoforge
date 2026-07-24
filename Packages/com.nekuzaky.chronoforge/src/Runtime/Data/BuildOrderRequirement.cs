using System;

namespace Chronoforge
{
    /// <summary>
    /// A prerequisite a step depends on. <see cref="m_TargetId"/> points at a step id,
    /// a resource id, or a free-form key depending on <see cref="m_Type"/>.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderRequirement
    {
        public BuildOrderRequirementType m_Type = BuildOrderRequirementType.Step;
        public string m_TargetId = "";
        public string m_Label = "";
        public float m_Amount;
        public bool m_Optional;

        public bool Matches(BuildOrderRequirement other) =>
            other != null && other.m_Type == m_Type && other.m_TargetId == m_TargetId;
    }
}
