using UnityEngine;

namespace Chronoforge.Tests
{
    /// <summary>Small builders so tests read as intent, not setup boilerplate.</summary>
    internal static class BuildOrderTestFactory
    {
        public static BuildOrderAsset Asset() => ScriptableObject.CreateInstance<BuildOrderAsset>();

        public static BuildOrderStep Step(
            string id,
            string title,
            int supply = 0,
            float time = 0f,
            int delta = 0,
            BuildOrderActionType type = BuildOrderActionType.Unit)
        {
            return new BuildOrderStep
            {
                m_Id = id,
                m_Title = title,
                m_Supply = supply,
                m_TimeSeconds = time,
                m_PopulationDelta = delta,
                m_Type = type
            };
        }

        public static bool HasCode(System.Collections.Generic.List<BuildOrderValidationIssue> issues, BuildOrderValidationCode code)
        {
            foreach (BuildOrderValidationIssue issue in issues)
            {
                if (issue.m_Code == code)
                    return true;
            }
            return false;
        }
    }
}
