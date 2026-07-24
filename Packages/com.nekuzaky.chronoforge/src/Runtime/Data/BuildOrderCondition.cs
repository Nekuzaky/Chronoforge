using System;

namespace Chronoforge
{
    /// <summary>
    /// A single boolean test used to gate a <see cref="BuildOrderBranch"/>, expressed as
    /// <c>{variable} {operator} {value}</c>. Evaluation is left to the host game; Chronoforge
    /// only authors and displays it.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderCondition
    {
        public string m_Variable = "";
        public BuildOrderConditionOperator m_Operator = BuildOrderConditionOperator.Equals;
        public string m_Value = "";
        public string m_Description = "";

        public bool IsEmpty => string.IsNullOrWhiteSpace(m_Variable);

        public string ToDisplayString() => m_Operator switch
        {
            BuildOrderConditionOperator.Exists => $"{m_Variable} exists",
            BuildOrderConditionOperator.NotExists => $"{m_Variable} missing",
            _ => $"{m_Variable} {OperatorSymbol()} {m_Value}"
        };

        private string OperatorSymbol() => m_Operator switch
        {
            BuildOrderConditionOperator.Equals => "==",
            BuildOrderConditionOperator.NotEquals => "!=",
            BuildOrderConditionOperator.Greater => ">",
            BuildOrderConditionOperator.GreaterOrEqual => ">=",
            BuildOrderConditionOperator.Less => "<",
            BuildOrderConditionOperator.LessOrEqual => "<=",
            _ => "?"
        };
    }
}
