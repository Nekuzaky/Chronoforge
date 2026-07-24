using System;
using System.Collections.Generic;

namespace Chronoforge
{
    /// <summary>
    /// A single planned action in a build order. Self-describing and id-stable so it can be
    /// referenced by requirements, reordered freely, and diffed across snapshots.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderStep
    {
        #region Identity
        public string m_Id = "";
        public string m_Title = "";
        public string m_Description = "";
        public BuildOrderActionType m_Type = BuildOrderActionType.Unit;
        #endregion

        #region Timing & supply
        public int m_Supply;
        public float m_TimeSeconds;
        public int m_PopulationRequirement;
        public int m_PopulationDelta;
        public float m_EstimatedDuration;
        #endregion

        #region Economy & dependencies
        public BuildOrderResourceCost m_ResourceCost = new();
        public List<BuildOrderRequirement> m_Prerequisites = new();
        #endregion

        #region Organisation
        public string m_BranchKey = "";
        public List<string> m_TagIds = new();
        public string m_DesignerNotes = "";
        public BuildOrderPriority m_Priority = BuildOrderPriority.Normal;
        public bool m_Optional;
        public bool m_Repeatable;
        public BuildOrderStepStatus m_Status = BuildOrderStepStatus.Planned;
        #endregion

        #region Cached validation (not authoritative — recomputed by the validator)
        public BuildOrderValidationState m_ValidationState = BuildOrderValidationState.Unknown;
        #endregion

        /// <summary>Formatted <c>M:SS</c> label for display; never persisted as source of truth.</summary>
        public string DisplayTime => BuildOrderTime.Format(m_TimeSeconds);

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(m_Title) &&
            string.IsNullOrWhiteSpace(m_Description) &&
            m_Type != BuildOrderActionType.Note;

        public static BuildOrderStep Create(BuildOrderActionType type) => new()
        {
            m_Id = Guid.NewGuid().ToString("N"),
            m_Type = type
        };

        public BuildOrderStep Clone()
        {
            var clone = (BuildOrderStep)MemberwiseClone();
            clone.m_Id = Guid.NewGuid().ToString("N");
            clone.m_ResourceCost = new BuildOrderResourceCost { m_Amounts = new List<BuildOrderResourceAmount>(m_ResourceCost.m_Amounts) };
            clone.m_Prerequisites = new List<BuildOrderRequirement>(m_Prerequisites);
            clone.m_TagIds = new List<string>(m_TagIds);
            return clone;
        }
    }
}
