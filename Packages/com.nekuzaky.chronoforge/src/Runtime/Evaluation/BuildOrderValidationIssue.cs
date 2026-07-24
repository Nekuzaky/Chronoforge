using System;

namespace Chronoforge
{
    /// <summary>One problem found by the validator, anchored to the step that raised it.</summary>
    [Serializable]
    public sealed class BuildOrderValidationIssue
    {
        public BuildOrderIssueSeverity m_Severity;
        public BuildOrderValidationCode m_Code;
        public string m_Message = "";
        public string m_StepId = "";
        public int m_StepIndex = -1;

        public BuildOrderValidationIssue() { }

        public BuildOrderValidationIssue(
            BuildOrderIssueSeverity severity,
            BuildOrderValidationCode code,
            string message,
            string stepId = "",
            int stepIndex = -1)
        {
            m_Severity = severity;
            m_Code = code;
            m_Message = message;
            m_StepId = stepId;
            m_StepIndex = stepIndex;
        }
    }
}
