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

        #region Production model (optional — powers the clean-build analysis)
        /// <summary>Supply cap this step adds (supply depot / overlord / pylon and the like).</summary>
        public int m_SupplyProvided;

        /// <summary>Set when this step creates a production facility; the id others produce from.</summary>
        public string m_ProvidesFacilityId = "";

        /// <summary>Set when this step is produced by a facility; must match a provider's id.</summary>
        public string m_ProducedByFacilityId = "";

        /// <summary>Marks worker/economy production, so gaps in it can be reported.</summary>
        public bool m_IsWorker;
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

        /// <summary>
        /// Deep copy with a fresh id. Nested cost and prerequisite entries are cloned too —
        /// copying only the lists would leave the copy's costs aliased to the original's, so
        /// editing a duplicated step would silently change the step it came from.
        /// </summary>
        public BuildOrderStep Clone()
        {
            var clone = (BuildOrderStep)MemberwiseClone();
            clone.m_Id = Guid.NewGuid().ToString("N");

            clone.m_ResourceCost = new BuildOrderResourceCost();
            for (int i = 0; i < m_ResourceCost.m_Amounts.Count; i++)
            {
                BuildOrderResourceAmount amount = m_ResourceCost.m_Amounts[i];
                clone.m_ResourceCost.m_Amounts.Add(new BuildOrderResourceAmount(amount.m_ResourceId, amount.m_Amount));
            }

            clone.m_Prerequisites = new List<BuildOrderRequirement>(m_Prerequisites.Count);
            for (int i = 0; i < m_Prerequisites.Count; i++)
            {
                BuildOrderRequirement source = m_Prerequisites[i];
                clone.m_Prerequisites.Add(new BuildOrderRequirement
                {
                    m_Type = source.m_Type,
                    m_TargetId = source.m_TargetId,
                    m_Label = source.m_Label,
                    m_Amount = source.m_Amount,
                    m_Optional = source.m_Optional
                });
            }

            clone.m_TagIds = new List<string>(m_TagIds);
            return clone;
        }
    }
}
