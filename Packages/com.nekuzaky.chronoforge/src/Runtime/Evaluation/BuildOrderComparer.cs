using System.Collections.Generic;
using UnityEngine;

namespace Chronoforge
{
    /// <summary>
    /// Diffs a baseline build order (planned) against the current one (actual), matching steps by
    /// id. Reports additions, removals, and per-step time / supply / type shifts. Stateless and
    /// UI-agnostic; the editor renders the result.
    /// </summary>
    public static class BuildOrderComparer
    {
        public static BuildOrderDiff Compare(BuildOrderAsset baseline, BuildOrderAsset current)
        {
            var diff = new BuildOrderDiff();
            if (current == null)
                return diff;

            Dictionary<string, BuildOrderStep> baselineById = IndexById(baseline);
            var matched = new HashSet<string>();

            foreach (BuildOrderStep step in current.m_Steps)
            {
                if (baselineById.TryGetValue(step.m_Id, out BuildOrderStep before))
                {
                    matched.Add(step.m_Id);
                    diff.m_Entries.Add(CompareStep(before, step, diff));
                }
                else
                {
                    diff.m_Added++;
                    diff.m_Entries.Add(new BuildOrderDiffEntry
                    {
                        m_StepId = step.m_Id,
                        m_Title = Title(step),
                        m_ChangeType = BuildOrderChangeType.Added,
                        m_Detail = "not in baseline"
                    });
                }
            }

            if (baseline != null)
            {
                foreach (BuildOrderStep before in baseline.m_Steps)
                {
                    if (matched.Contains(before.m_Id))
                        continue;

                    diff.m_Removed++;
                    diff.m_Entries.Add(new BuildOrderDiffEntry
                    {
                        m_StepId = before.m_Id,
                        m_Title = Title(before),
                        m_ChangeType = BuildOrderChangeType.Removed,
                        m_Detail = "removed from build"
                    });
                }
            }

            return diff;
        }

        private static BuildOrderDiffEntry CompareStep(BuildOrderStep before, BuildOrderStep after, BuildOrderDiff diff)
        {
            var entry = new BuildOrderDiffEntry
            {
                m_StepId = after.m_Id,
                m_Title = Title(after)
            };

            if (!Mathf.Approximately(before.m_TimeSeconds, after.m_TimeSeconds))
            {
                entry.m_ChangeType = BuildOrderChangeType.TimeShift;
                entry.m_DeltaSeconds = after.m_TimeSeconds - before.m_TimeSeconds;
                string sign = entry.m_DeltaSeconds >= 0f ? "+" : "-";
                entry.m_Detail = $"time {before.DisplayTime} → {after.DisplayTime} ({sign}{BuildOrderTime.Format(Mathf.Abs(entry.m_DeltaSeconds))})";
                diff.m_Changed++;
            }
            else if (before.m_Supply != after.m_Supply)
            {
                entry.m_ChangeType = BuildOrderChangeType.SupplyShift;
                entry.m_Detail = $"supply {before.m_Supply} → {after.m_Supply}";
                diff.m_Changed++;
            }
            else if (before.m_Type != after.m_Type)
            {
                entry.m_ChangeType = BuildOrderChangeType.Retyped;
                entry.m_Detail = $"type {before.m_Type} → {after.m_Type}";
                diff.m_Changed++;
            }
            else
            {
                entry.m_ChangeType = BuildOrderChangeType.Unchanged;
            }

            return entry;
        }

        private static Dictionary<string, BuildOrderStep> IndexById(BuildOrderAsset asset)
        {
            var map = new Dictionary<string, BuildOrderStep>();
            if (asset == null)
                return map;

            foreach (BuildOrderStep step in asset.m_Steps)
            {
                if (!string.IsNullOrEmpty(step.m_Id))
                    map[step.m_Id] = step;
            }
            return map;
        }

        private static string Title(BuildOrderStep step) =>
            string.IsNullOrWhiteSpace(step.m_Title) ? $"({step.m_Type})" : step.m_Title;
    }
}
