using System.Collections.Generic;
using UnityEngine;

namespace Chronoforge
{
    /// <summary>
    /// Execution-quality analysis — the "clean build" checks: never supply-blocked, production
    /// never idle, resources never stockpiling, worker production never interrupted. Pure and
    /// deterministic; writes issues into the caller's list.
    /// <para>
    /// Written to the JPL Power-of-10 rules (see Documentation~/coding-standard.md): no recursion,
    /// every loop bounded by a collection count, working state allocated once per call and never
    /// inside a loop, parameters validated, each check under a screen.
    /// </para>
    /// </summary>
    public static class BuildOrderCleanBuildAnalyzer
    {
        /// <summary>Hard ceiling on reports per rule, so one bad build can't flood the panel.</summary>
        private const int k_MaxIssuesPerRule = 12;

        public static void Analyze(BuildOrderAsset asset, BuildOrderEvaluationResult result)
        {
            // Rule 7: validate parameters, return a defined result instead of throwing.
            if (asset == null || result == null)
                return;
            if (asset.m_CleanBuild == null || !asset.m_CleanBuild.m_Enabled)
                return;

            // Rule 5: side-effect-free invariant assertions.
            Debug.Assert(asset.m_Steps != null, "Build order steps list must never be null.");
            Debug.Assert(result.m_Issues != null, "Evaluation issue list must never be null.");

            BuildOrderCleanBuildSettings settings = asset.m_CleanBuild;

            if (settings.m_CheckSupplyBlocks)
                CheckSupplyBlocks(asset, settings, result.m_Issues);
            if (settings.m_CheckWorkerGaps)
                CheckWorkerGaps(asset, settings, result.m_Issues);
            if (settings.m_CheckIdleProduction)
                CheckIdleProduction(asset, settings, result.m_Issues);
            if (settings.m_CheckStockpiling)
                CheckStockpiling(asset, settings, result.m_Issues);
        }

        #region Supply
        /// <summary>
        /// Walks the build tracking supply used against the cap, and reports the steps that would
        /// be blocked because no supply structure landed in time.
        /// </summary>
        private static void CheckSupplyBlocks(
            BuildOrderAsset asset, BuildOrderCleanBuildSettings settings, List<BuildOrderValidationIssue> issues)
        {
            int cap = settings.m_StartingSupplyCap;
            int used = 0;
            int reported = 0;

            for (int i = 0; i < asset.m_Steps.Count; i++)
            {
                BuildOrderStep step = asset.m_Steps[i];
                cap += step.m_SupplyProvided;

                bool blocked = step.m_PopulationDelta > 0 && used + step.m_PopulationDelta > cap;
                if (blocked && reported < k_MaxIssuesPerRule)
                {
                    reported++;
                    issues.Add(new BuildOrderValidationIssue(
                        BuildOrderIssueSeverity.Warning,
                        BuildOrderValidationCode.SupplyBlock,
                        $"Supply blocked at {step.DisplayTime}: '{Name(step)}' needs {used + step.m_PopulationDelta} of {cap} supply.",
                        step.m_Id,
                        i));
                }

                used += step.m_PopulationDelta;
            }
        }
        #endregion

        #region Workers
        /// <summary>Reports gaps between worker steps longer than the configured tolerance.</summary>
        private static void CheckWorkerGaps(
            BuildOrderAsset asset, BuildOrderCleanBuildSettings settings, List<BuildOrderValidationIssue> issues)
        {
            float previousWorkerTime = float.NegativeInfinity;
            int reported = 0;

            for (int i = 0; i < asset.m_Steps.Count; i++)
            {
                BuildOrderStep step = asset.m_Steps[i];
                if (!step.m_IsWorker)
                    continue;

                bool hasPrevious = previousWorkerTime > float.NegativeInfinity;
                float gap = step.m_TimeSeconds - previousWorkerTime;
                if (hasPrevious && gap > settings.m_MaxWorkerGapSeconds && reported < k_MaxIssuesPerRule)
                {
                    reported++;
                    issues.Add(new BuildOrderValidationIssue(
                        BuildOrderIssueSeverity.Warning,
                        BuildOrderValidationCode.WorkerProductionGap,
                        $"Worker production paused {BuildOrderTime.Format(gap)} before {step.DisplayTime}.",
                        step.m_Id,
                        i));
                }

                previousWorkerTime = step.m_TimeSeconds;
            }
        }
        #endregion

        #region Production facilities
        /// <summary>
        /// For every facility a step provides, reports when it sits unused longer than tolerated
        /// before the next step it produces.
        /// </summary>
        private static void CheckIdleProduction(
            BuildOrderAsset asset, BuildOrderCleanBuildSettings settings, List<BuildOrderValidationIssue> issues)
        {
            // Rule 3 (adapted): working state allocated once, never inside the loops below.
            var lastActivity = new Dictionary<string, float>();
            int reported = 0;

            for (int i = 0; i < asset.m_Steps.Count; i++)
            {
                BuildOrderStep step = asset.m_Steps[i];

                if (!string.IsNullOrEmpty(step.m_ProvidesFacilityId))
                    lastActivity[step.m_ProvidesFacilityId] = step.m_TimeSeconds + step.m_EstimatedDuration;

                string facility = step.m_ProducedByFacilityId;
                if (string.IsNullOrEmpty(facility))
                    continue;

                // Rule 7: check the lookup result rather than assuming presence.
                if (!lastActivity.TryGetValue(facility, out float availableSince))
                {
                    if (reported < k_MaxIssuesPerRule)
                    {
                        reported++;
                        issues.Add(new BuildOrderValidationIssue(
                            BuildOrderIssueSeverity.Error,
                            BuildOrderValidationCode.IdleProduction,
                            $"'{Name(step)}' is produced by '{facility}', which no earlier step provides.",
                            step.m_Id,
                            i));
                    }
                    continue;
                }

                float idle = step.m_TimeSeconds - availableSince;
                if (idle > settings.m_MaxFacilityIdleSeconds && reported < k_MaxIssuesPerRule)
                {
                    reported++;
                    issues.Add(new BuildOrderValidationIssue(
                        BuildOrderIssueSeverity.Warning,
                        BuildOrderValidationCode.IdleProduction,
                        $"'{facility}' idle {BuildOrderTime.Format(idle)} before {step.DisplayTime}.",
                        step.m_Id,
                        i));
                }

                lastActivity[facility] = step.m_TimeSeconds + step.m_EstimatedDuration;
            }
        }
        #endregion

        #region Economy
        /// <summary>
        /// Projects each modelled resource forward and reports the first moment a balance sits
        /// above the stockpile threshold — money that should have been spent.
        /// </summary>
        private static void CheckStockpiling(
            BuildOrderAsset asset, BuildOrderCleanBuildSettings settings, List<BuildOrderValidationIssue> issues)
        {
            if (asset.m_ResourceModel.Count == 0)
                return;

            int reported = 0;

            for (int r = 0; r < asset.m_ResourceModel.Count; r++)
            {
                BuildOrderResourceRate rate = asset.m_ResourceModel[r];
                if (string.IsNullOrEmpty(rate.m_ResourceId))
                    continue;
                if (reported >= k_MaxIssuesPerRule)
                    break;

                if (TryFindStockpile(asset, rate, settings.m_StockpileThreshold, out int stepIndex, out float balance))
                {
                    reported++;
                    BuildOrderStep step = asset.m_Steps[stepIndex];
                    issues.Add(new BuildOrderValidationIssue(
                        BuildOrderIssueSeverity.Info,
                        BuildOrderValidationCode.ResourceStockpiling,
                        $"{rate.m_ResourceId} reaches {Mathf.RoundToInt(balance)} by {step.DisplayTime} — spend it earlier.",
                        step.m_Id,
                        stepIndex));
                }
            }
        }

        /// <summary>
        /// Simulates one resource across the build. Returns the first step where the projected
        /// balance exceeds <paramref name="threshold"/>.
        /// </summary>
        private static bool TryFindStockpile(
            BuildOrderAsset asset,
            BuildOrderResourceRate rate,
            float threshold,
            out int stepIndex,
            out float balance)
        {
            stepIndex = -1;
            balance = rate.m_StartingAmount;
            float previousTime = 0f;

            for (int i = 0; i < asset.m_Steps.Count; i++)
            {
                BuildOrderStep step = asset.m_Steps[i];
                float elapsed = Mathf.Max(0f, step.m_TimeSeconds - previousTime);
                balance += rate.m_IncomePerSecond * elapsed;
                previousTime = step.m_TimeSeconds;

                if (balance > threshold)
                {
                    stepIndex = i;
                    return true;
                }

                balance -= step.m_ResourceCost.GetAmount(rate.m_ResourceId);
            }

            return false;
        }
        #endregion

        private static string Name(BuildOrderStep step) =>
            string.IsNullOrWhiteSpace(step.m_Title) ? $"({step.m_Type})" : step.m_Title;
    }
}
