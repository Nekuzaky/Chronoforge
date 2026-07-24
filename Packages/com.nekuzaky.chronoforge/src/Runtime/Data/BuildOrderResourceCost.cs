using System;
using System.Collections.Generic;

namespace Chronoforge
{
    /// <summary>
    /// One resource line, e.g. <c>("minerals", 150)</c>. Resource ids are free-form
    /// strings so no economy is baked into the tool.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderResourceAmount
    {
        public string m_ResourceId = "";
        public float m_Amount;

        public BuildOrderResourceAmount() { }

        public BuildOrderResourceAmount(string resourceId, float amount)
        {
            m_ResourceId = resourceId;
            m_Amount = amount;
        }
    }

    /// <summary>Aggregate cost of a step across any number of resources.</summary>
    [Serializable]
    public sealed class BuildOrderResourceCost
    {
        public List<BuildOrderResourceAmount> m_Amounts = new();

        public bool IsEmpty => m_Amounts.Count == 0;

        public float GetAmount(string resourceId)
        {
            foreach (BuildOrderResourceAmount amount in m_Amounts)
            {
                if (amount.m_ResourceId == resourceId)
                    return amount.m_Amount;
            }
            return 0f;
        }
    }

    /// <summary>
    /// Optional per-resource economy model on the asset: a starting stockpile and a
    /// flat income rate. Powers the lightweight resource simulation.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderResourceRate
    {
        public string m_ResourceId = "";
        public float m_StartingAmount;
        public float m_IncomePerSecond;
    }
}
