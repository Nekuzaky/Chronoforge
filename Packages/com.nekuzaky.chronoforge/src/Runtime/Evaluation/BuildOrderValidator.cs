using System.Collections.Generic;

namespace Chronoforge
{
    /// <summary>
    /// Stateless rule engine. Runs every check against an asset and returns a flat list of
    /// issues; never mutates the asset. UI-agnostic and unit-testable.
    /// </summary>
    public static class BuildOrderValidator
    {
        public static List<BuildOrderValidationIssue> Validate(BuildOrderAsset asset)
        {
            var issues = new List<BuildOrderValidationIssue>();
            if (asset == null)
                return issues;

            var seenIds = new HashSet<string>();
            var stepIds = CollectStepIds(asset);
            var branchKeys = CollectBranchKeys(asset);

            float previousTime = float.NegativeInfinity;
            int previousSupply = int.MinValue;

            for (int index = 0; index < asset.m_Steps.Count; index++)
            {
                BuildOrderStep step = asset.m_Steps[index];

                ValidateIdentity(step, index, seenIds, issues);
                ValidateContent(step, index, issues);
                ValidateTiming(step, index, previousTime, previousSupply, issues);
                ValidateCost(step, index, issues);
                ValidatePrerequisites(step, index, stepIds, issues);
                ValidateBranch(step, index, branchKeys, issues);

                previousTime = step.m_TimeSeconds;
                previousSupply = step.m_Supply;
            }

            return issues;
        }

        #region Rules
        private static void ValidateIdentity(
            BuildOrderStep step, int index, HashSet<string> seenIds, List<BuildOrderValidationIssue> issues)
        {
            if (string.IsNullOrEmpty(step.m_Id))
            {
                Add(issues, BuildOrderIssueSeverity.Error, BuildOrderValidationCode.IncompleteData,
                    "Step has no id.", step, index);
                return;
            }

            if (!seenIds.Add(step.m_Id))
            {
                Add(issues, BuildOrderIssueSeverity.Error, BuildOrderValidationCode.DuplicateId,
                    $"Duplicate step id '{step.m_Id}'.", step, index);
            }
        }

        private static void ValidateContent(BuildOrderStep step, int index, List<BuildOrderValidationIssue> issues)
        {
            if (step.IsEmpty)
            {
                Add(issues, BuildOrderIssueSeverity.Warning, BuildOrderValidationCode.EmptyStep,
                    "Step has no title or description.", step, index);
            }
        }

        private static void ValidateTiming(
            BuildOrderStep step, int index, float previousTime, int previousSupply, List<BuildOrderValidationIssue> issues)
        {
            if (step.m_TimeSeconds < 0f)
            {
                Add(issues, BuildOrderIssueSeverity.Error, BuildOrderValidationCode.NegativeTime,
                    "Step time is negative.", step, index);
            }
            else if (previousTime > float.NegativeInfinity && step.m_TimeSeconds < previousTime)
            {
                Add(issues, BuildOrderIssueSeverity.Warning, BuildOrderValidationCode.NonMonotonicTime,
                    $"Step time {step.DisplayTime} is earlier than the previous step.", step, index);
            }

            if (step.m_Supply < 0)
            {
                Add(issues, BuildOrderIssueSeverity.Error, BuildOrderValidationCode.ImpossibleSupply,
                    "Supply is negative.", step, index);
            }
            else if (previousSupply > int.MinValue && step.m_Supply < previousSupply)
            {
                Add(issues, BuildOrderIssueSeverity.Info, BuildOrderValidationCode.SuspiciousOrder,
                    $"Supply {step.m_Supply} drops below the previous step ({previousSupply}).", step, index);
            }
        }

        private static void ValidateCost(BuildOrderStep step, int index, List<BuildOrderValidationIssue> issues)
        {
            foreach (BuildOrderResourceAmount amount in step.m_ResourceCost.m_Amounts)
            {
                if (amount.m_Amount < 0f)
                {
                    Add(issues, BuildOrderIssueSeverity.Error, BuildOrderValidationCode.NegativeCost,
                        $"Resource '{amount.m_ResourceId}' has a negative cost.", step, index);
                }
            }
        }

        private static void ValidatePrerequisites(
            BuildOrderStep step, int index, HashSet<string> stepIds, List<BuildOrderValidationIssue> issues)
        {
            var seen = new HashSet<string>();
            foreach (BuildOrderRequirement requirement in step.m_Prerequisites)
            {
                string key = $"{requirement.m_Type}:{requirement.m_TargetId}";
                if (!seen.Add(key))
                {
                    Add(issues, BuildOrderIssueSeverity.Warning, BuildOrderValidationCode.DuplicatePrerequisite,
                        $"Duplicate prerequisite '{requirement.m_TargetId}'.", step, index);
                }

                bool pointsAtStep = requirement.m_Type == BuildOrderRequirementType.Step;
                if (pointsAtStep && !string.IsNullOrEmpty(requirement.m_TargetId) && !stepIds.Contains(requirement.m_TargetId))
                {
                    Add(issues, BuildOrderIssueSeverity.Error, BuildOrderValidationCode.MissingPrerequisite,
                        $"Prerequisite step '{requirement.m_TargetId}' does not exist.", step, index);
                }
            }
        }

        private static void ValidateBranch(
            BuildOrderStep step, int index, HashSet<string> branchKeys, List<BuildOrderValidationIssue> issues)
        {
            if (!string.IsNullOrEmpty(step.m_BranchKey) && !branchKeys.Contains(step.m_BranchKey))
            {
                Add(issues, BuildOrderIssueSeverity.Error, BuildOrderValidationCode.BrokenBranch,
                    $"Branch '{step.m_BranchKey}' is not defined on the asset.", step, index);
            }
        }
        #endregion

        #region Helpers
        private static HashSet<string> CollectStepIds(BuildOrderAsset asset)
        {
            var ids = new HashSet<string>();
            foreach (BuildOrderStep step in asset.m_Steps)
            {
                if (!string.IsNullOrEmpty(step.m_Id))
                    ids.Add(step.m_Id);
            }
            return ids;
        }

        private static HashSet<string> CollectBranchKeys(BuildOrderAsset asset)
        {
            var keys = new HashSet<string>();
            foreach (BuildOrderBranch branch in asset.m_Branches)
            {
                if (!string.IsNullOrEmpty(branch.m_Key))
                    keys.Add(branch.m_Key);
            }
            return keys;
        }

        private static void Add(
            List<BuildOrderValidationIssue> issues,
            BuildOrderIssueSeverity severity,
            BuildOrderValidationCode code,
            string message,
            BuildOrderStep step,
            int index) =>
            issues.Add(new BuildOrderValidationIssue(severity, code, message, step.m_Id, index));
        #endregion
    }
}
