using System.Collections.Generic;

namespace Chronoforge
{
    /// <summary>
    /// Lightweight forward simulation of a build order: derives a timeline from each step's
    /// time and estimated duration, accumulates supply from population deltas, and — when the
    /// asset defines a resource model — flags steps the economy could not afford.
    /// Deterministic and side-effect free.
    /// </summary>
    public static class BuildOrderSimulator
    {
        public static BuildOrderEvaluationResult Evaluate(BuildOrderAsset asset)
        {
            var result = new BuildOrderEvaluationResult();
            if (asset == null)
                return result;

            Dictionary<string, float> balances = BuildStartingBalances(asset);
            Dictionary<string, float> income = BuildIncomeRates(asset);

            int runningSupply = 0;
            float lastEnd = 0f;

            for (int index = 0; index < asset.m_Steps.Count; index++)
            {
                BuildOrderStep step = asset.m_Steps[index];
                runningSupply += step.m_PopulationDelta;

                var entry = new BuildOrderTimelineEntry
                {
                    m_StepId = step.m_Id,
                    m_StepIndex = index,
                    m_StartSeconds = step.m_TimeSeconds,
                    m_EndSeconds = step.m_TimeSeconds + step.m_EstimatedDuration,
                    m_ProjectedSupply = runningSupply,
                    m_HasResourceShortfall = IsUnaffordable(step, balances, income)
                };

                if (entry.m_HasResourceShortfall)
                {
                    result.m_Issues.Add(new BuildOrderValidationIssue(
                        BuildOrderIssueSeverity.Warning,
                        BuildOrderValidationCode.ResourceShortfall,
                        $"Economy cannot afford '{step.m_Title}' at {step.DisplayTime}.",
                        step.m_Id,
                        index));
                }

                ApplyCost(step, balances);
                result.m_Timeline.Add(entry);
                lastEnd = entry.m_EndSeconds > lastEnd ? entry.m_EndSeconds : lastEnd;
            }

            result.m_TotalSeconds = lastEnd;
            result.m_FinalSupply = runningSupply;
            return result;
        }

        #region Economy
        private static bool IsUnaffordable(
            BuildOrderStep step, Dictionary<string, float> balances, Dictionary<string, float> income)
        {
            if (step.m_ResourceCost.IsEmpty || balances.Count == 0)
                return false;

            foreach (BuildOrderResourceAmount cost in step.m_ResourceCost.m_Amounts)
            {
                if (!balances.TryGetValue(cost.m_ResourceId, out float balance))
                    continue;

                float rate = income.TryGetValue(cost.m_ResourceId, out float perSecond) ? perSecond : 0f;
                float available = balance + rate * step.m_TimeSeconds;
                if (available < cost.m_Amount)
                    return true;
            }
            return false;
        }

        private static void ApplyCost(BuildOrderStep step, Dictionary<string, float> balances)
        {
            foreach (BuildOrderResourceAmount cost in step.m_ResourceCost.m_Amounts)
            {
                if (balances.ContainsKey(cost.m_ResourceId))
                    balances[cost.m_ResourceId] -= cost.m_Amount;
            }
        }

        private static Dictionary<string, float> BuildStartingBalances(BuildOrderAsset asset)
        {
            var balances = new Dictionary<string, float>();
            foreach (BuildOrderResourceRate rate in asset.m_ResourceModel)
            {
                if (!string.IsNullOrEmpty(rate.m_ResourceId))
                    balances[rate.m_ResourceId] = rate.m_StartingAmount;
            }
            return balances;
        }

        private static Dictionary<string, float> BuildIncomeRates(BuildOrderAsset asset)
        {
            var income = new Dictionary<string, float>();
            foreach (BuildOrderResourceRate rate in asset.m_ResourceModel)
            {
                if (!string.IsNullOrEmpty(rate.m_ResourceId))
                    income[rate.m_ResourceId] = rate.m_IncomePerSecond;
            }
            return income;
        }
        #endregion
    }
}
