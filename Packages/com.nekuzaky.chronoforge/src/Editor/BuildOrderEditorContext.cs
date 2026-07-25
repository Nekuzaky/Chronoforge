using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Single source of truth shared by every editor panel. Owns the loaded asset, the
    /// selection, the active search filter, and the cached evaluation. Panels never mutate the
    /// asset directly — they call these methods, which handle Undo, dirtying and change
    /// notification so the whole window stays consistent.
    /// </summary>
    public sealed class BuildOrderEditorContext
    {
        #region Events
        /// <summary>Raised after any structural or field change; panels rebuild from it.</summary>
        public event Action Changed;

        /// <summary>Raised when the selected step changes without a data change.</summary>
        public event Action SelectionChanged;
        #endregion

        #region State
        public BuildOrderAsset m_Asset;
        public BuildOrderEvaluationResult m_Evaluation = new();

        /// <summary>Selected step ids in click order; the last one is the primary (edited) step.</summary>
        private readonly List<string> _selectedIds = new();
        private string _search = "";
        #endregion

        #region Selection & filtering
        public string Search
        {
            get => _search;
            set
            {
                if (_search == value)
                    return;
                _search = value ?? "";
                RaiseChanged();
            }
        }

        public bool HasFilter => !string.IsNullOrEmpty(_search);

        /// <summary>The step the details panel edits — the most recently selected one.</summary>
        public BuildOrderStep SelectedStep
        {
            get
            {
                string primary = PrimaryId;
                return m_Asset != null && !string.IsNullOrEmpty(primary) ? m_Asset.FindStep(primary) : null;
            }
        }

        public string PrimaryId => _selectedIds.Count > 0 ? _selectedIds[^1] : "";

        public int SelectionCount => _selectedIds.Count;

        public int SelectedIndex =>
            m_Asset != null ? m_Asset.m_Steps.FindIndex(step => step.m_Id == PrimaryId) : -1;

        public void Select(string stepId)
        {
            if (_selectedIds.Count == 1 && _selectedIds[0] == stepId)
                return;

            _selectedIds.Clear();
            if (!string.IsNullOrEmpty(stepId))
                _selectedIds.Add(stepId);
            SelectionChanged?.Invoke();
        }

        /// <summary>Replaces the whole selection, preserving the given order (last = primary).</summary>
        public void SetSelection(List<string> stepIds)
        {
            if (SelectionMatches(stepIds))
                return;

            _selectedIds.Clear();
            if (stepIds != null)
            {
                for (int i = 0; i < stepIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(stepIds[i]))
                        _selectedIds.Add(stepIds[i]);
                }
            }
            SelectionChanged?.Invoke();
        }

        /// <summary>Selected steps in build order, not click order — callers act on them in sequence.</summary>
        public List<BuildOrderStep> GetSelectedSteps()
        {
            var selected = new List<BuildOrderStep>();
            if (m_Asset == null)
                return selected;

            for (int i = 0; i < m_Asset.m_Steps.Count; i++)
            {
                if (_selectedIds.Contains(m_Asset.m_Steps[i].m_Id))
                    selected.Add(m_Asset.m_Steps[i]);
            }
            return selected;
        }

        private bool SelectionMatches(List<string> stepIds)
        {
            int incoming = stepIds?.Count ?? 0;
            if (incoming != _selectedIds.Count)
                return false;

            for (int i = 0; i < _selectedIds.Count; i++)
            {
                if (_selectedIds[i] != stepIds[i])
                    return false;
            }
            return true;
        }

        /// <summary>Steps to display given the current filter. Returns the live list when unfiltered.</summary>
        public List<BuildOrderStep> GetVisibleSteps()
        {
            if (m_Asset == null)
                return new List<BuildOrderStep>();
            if (!HasFilter)
                return m_Asset.m_Steps;

            string needle = _search.ToLowerInvariant();
            var filtered = new List<BuildOrderStep>();
            foreach (BuildOrderStep step in m_Asset.m_Steps)
            {
                if (MatchesFilter(step, needle))
                    filtered.Add(step);
            }
            return filtered;
        }
        #endregion

        #region Lifecycle
        public void Load(BuildOrderAsset asset)
        {
            m_Asset = asset;
            SelectOnly(asset != null && asset.m_Steps.Count > 0 ? asset.m_Steps[0].m_Id : "");
            Reevaluate();
            RaiseChanged();
        }
        #endregion

        #region Mutations
        /// <summary>
        /// Inserts a new step after the current selection — you almost always want the next step
        /// where you are working, not at the end of the list — inheriting supply/time/branch from
        /// the step it follows. Appends when nothing is selected.
        /// </summary>
        public void AddStep(BuildOrderActionType type)
        {
            if (m_Asset == null)
                return;

            RecordUndo("Add Build Order Step");

            int after = SelectedIndex;
            BuildOrderStep template = after >= 0
                ? m_Asset.m_Steps[after]
                : m_Asset.m_Steps.Count > 0 ? m_Asset.m_Steps[^1] : null;

            BuildOrderStep step = BuildOrderStep.Create(type);
            if (template != null)
            {
                step.m_Supply = template.m_Supply + 1;
                step.m_TimeSeconds = template.m_TimeSeconds;
                step.m_BranchKey = template.m_BranchKey;
            }

            if (after >= 0)
                m_Asset.m_Steps.Insert(after + 1, step);
            else
                m_Asset.m_Steps.Add(step);

            SelectOnly(step.m_Id);
            RaiseChanged();
        }

        /// <summary>Duplicates every selected step, keeping the copies together after the originals.</summary>
        public void DuplicateSelected()
        {
            List<BuildOrderStep> selected = GetSelectedSteps();
            if (selected.Count == 0)
                return;

            RecordUndo("Duplicate Build Order Steps");

            int insertAt = m_Asset.m_Steps.IndexOf(selected[^1]) + 1;
            _selectedIds.Clear();
            for (int i = 0; i < selected.Count; i++)
            {
                BuildOrderStep clone = selected[i].Clone();
                m_Asset.m_Steps.Insert(insertAt + i, clone);
                _selectedIds.Add(clone.m_Id);
            }

            RaiseChanged();
        }

        /// <summary>Deletes every selected step and selects the nearest survivor.</summary>
        public void RemoveSelected()
        {
            List<BuildOrderStep> selected = GetSelectedSteps();
            if (selected.Count == 0)
                return;

            RecordUndo("Delete Build Order Steps");

            int firstIndex = m_Asset.m_Steps.IndexOf(selected[0]);
            for (int i = 0; i < selected.Count; i++)
                m_Asset.m_Steps.Remove(selected[i]);

            _selectedIds.Clear();
            if (m_Asset.m_Steps.Count > 0)
            {
                int next = Mathf.Clamp(firstIndex, 0, m_Asset.m_Steps.Count - 1);
                _selectedIds.Add(m_Asset.m_Steps[next].m_Id);
            }

            RaiseChanged();
        }

        /// <summary>Moves the selection by <paramref name="offset"/> rows for keyboard navigation.</summary>
        public void MoveSelection(int offset)
        {
            if (m_Asset == null || m_Asset.m_Steps.Count == 0)
                return;

            int index = SelectedIndex;
            int next = index < 0
                ? (offset > 0 ? 0 : m_Asset.m_Steps.Count - 1)
                : Mathf.Clamp(index + offset, 0, m_Asset.m_Steps.Count - 1);

            Select(m_Asset.m_Steps[next].m_Id);
        }

        private void SelectOnly(string stepId)
        {
            _selectedIds.Clear();
            if (!string.IsNullOrEmpty(stepId))
                _selectedIds.Add(stepId);
        }

        public void SortBySupply()
        {
            if (m_Asset == null)
                return;
            RecordUndo("Sort Build Order");
            m_Asset.m_Steps.Sort((a, b) =>
            {
                int bySupply = a.m_Supply.CompareTo(b.m_Supply);
                return bySupply != 0 ? bySupply : a.m_TimeSeconds.CompareTo(b.m_TimeSeconds);
            });
            RaiseChanged();
        }

        /// <summary>
        /// Flags the asset dirty, re-evaluates and notifies panels. Does NOT record Undo —
        /// callers record once before mutating so a single edit is one undo step.
        /// </summary>
        public void NotifyChanged()
        {
            if (m_Asset != null)
                EditorUtility.SetDirty(m_Asset);
            Reevaluate();
            Changed?.Invoke();
        }

        /// <summary>Call after a ListView drag reorder mutated the live list.</summary>
        public void NotifyReordered() => NotifyChanged();

        public void AddBenchmark()
        {
            if (m_Asset == null)
                return;

            RecordUndo("Add Benchmark");
            float anchor = m_Evaluation.m_TotalSeconds > 0f ? m_Evaluation.m_TotalSeconds : 60f;
            m_Asset.m_Benchmarks.Add(new BuildOrderBenchmark
            {
                m_Label = $"Checkpoint {m_Asset.m_Benchmarks.Count + 1}",
                m_AnchorTimeSeconds = anchor,
                m_CheckSupply = true,
                m_ExpectedSupply = m_Evaluation.m_FinalSupply
            });
            NotifyChanged();
        }

        public void RemoveBenchmark(BuildOrderBenchmark benchmark)
        {
            if (m_Asset == null || benchmark == null)
                return;

            RecordUndo("Remove Benchmark");
            m_Asset.m_Benchmarks.Remove(benchmark);
            NotifyChanged();
        }

        #region Clipboard
        /// <summary>Copies the selection to the system clipboard. Returns the number copied.</summary>
        public int CopySelection()
        {
            List<BuildOrderStep> selected = GetSelectedSteps();
            string payload = BuildOrderStepClipboard.ExportSteps(selected);
            if (string.IsNullOrEmpty(payload))
                return 0;

            EditorGUIUtility.systemCopyBuffer = payload;
            return selected.Count;
        }

        public int CutSelection()
        {
            int copied = CopySelection();
            if (copied > 0)
                RemoveSelected();
            return copied;
        }

        /// <summary>
        /// Pastes clipboard steps after the selection, with fresh ids. Returns false with an error
        /// when the clipboard holds anything other than Chronoforge steps.
        /// </summary>
        public bool PasteAfterSelection(out string error)
        {
            error = "";
            if (m_Asset == null)
                return false;

            if (!BuildOrderStepClipboard.TryImportSteps(EditorGUIUtility.systemCopyBuffer, out List<BuildOrderStep> pasted, out error))
                return false;

            RecordUndo("Paste Build Order Steps");

            int index = SelectedIndex;
            int insertAt = index >= 0 ? index + 1 : m_Asset.m_Steps.Count;

            _selectedIds.Clear();
            for (int i = 0; i < pasted.Count; i++)
            {
                m_Asset.m_Steps.Insert(insertAt + i, pasted[i]);
                _selectedIds.Add(pasted[i].m_Id);
            }

            RaiseChanged();
            return true;
        }
        #endregion

        #region Templates
        /// <summary>
        /// Replaces the steps of this build order with copies from <paramref name="template"/>.
        /// Metadata, benchmarks and history stay as they are — the template supplies a sequence,
        /// not an identity.
        /// </summary>
        public void ApplyTemplate(BuildOrderAsset template, bool replaceExisting)
        {
            if (m_Asset == null || template == null)
                return;

            RecordUndo("Apply Build Order Template");

            if (replaceExisting)
                m_Asset.m_Steps.Clear();

            _selectedIds.Clear();
            for (int i = 0; i < template.m_Steps.Count; i++)
            {
                BuildOrderStep clone = template.m_Steps[i].Clone();
                m_Asset.m_Steps.Add(clone);
                _selectedIds.Add(clone.m_Id);
            }

            // Bring branches the template's steps reference along, or they'd fail validation.
            for (int i = 0; i < template.m_Branches.Count; i++)
            {
                BuildOrderBranch branch = template.m_Branches[i];
                if (m_Asset.FindBranch(branch.m_Key) == null)
                    m_Asset.m_Branches.Add(branch);
            }

            RaiseChanged();
        }
        #endregion

        /// <summary>
        /// Overwrites the build order from a snapshot while keeping the current history intact —
        /// the snapshot payload is stored without its own history, so it must be re-attached.
        /// Returns false with an error when the payload is unreadable.
        /// </summary>
        public bool RestoreSnapshot(BuildOrderSnapshot snapshot, out string error)
        {
            error = "";
            if (m_Asset == null || snapshot == null)
                return false;

            RecordUndo("Restore Snapshot");
            var preservedHistory = new List<BuildOrderSnapshot>(m_Asset.m_Snapshots);
            if (!BuildOrderSerializer.ImportJson(snapshot.m_Json, m_Asset, out error))
                return false;

            m_Asset.m_Snapshots = preservedHistory;
            Load(m_Asset);
            return true;
        }

        public void DeleteSnapshot(BuildOrderSnapshot snapshot)
        {
            if (m_Asset == null || snapshot == null)
                return;

            RecordUndo("Delete Snapshot");
            m_Asset.m_Snapshots.Remove(snapshot);
            NotifyChanged();
        }

        public void RecordUndo(string name)
        {
            if (m_Asset != null)
                Undo.RecordObject(m_Asset, name);
        }
        #endregion

        #region Evaluation
        public void Reevaluate()
        {
            if (m_Asset == null)
            {
                m_Evaluation = new BuildOrderEvaluationResult();
                return;
            }

            m_Evaluation = BuildOrderEvaluation.Run(m_Asset);
            ApplyValidationStates();
        }
        #endregion

        #region Helpers
        private void RaiseChanged() => NotifyChanged();

        private void ApplyValidationStates()
        {
            foreach (BuildOrderStep step in m_Asset.m_Steps)
                step.m_ValidationState = BuildOrderValidationState.Valid;

            foreach (BuildOrderValidationIssue issue in m_Evaluation.m_Issues)
            {
                BuildOrderStep step = m_Asset.FindStep(issue.m_StepId);
                if (step == null)
                    continue;

                if (issue.m_Severity == BuildOrderIssueSeverity.Error)
                    step.m_ValidationState = BuildOrderValidationState.Error;
                else if (issue.m_Severity == BuildOrderIssueSeverity.Warning &&
                         step.m_ValidationState != BuildOrderValidationState.Error)
                    step.m_ValidationState = BuildOrderValidationState.Warning;
            }
        }

        private static bool MatchesFilter(BuildOrderStep step, string needle) =>
            step.m_Title.ToLowerInvariant().Contains(needle) ||
            step.m_Type.ToString().ToLowerInvariant().Contains(needle) ||
            step.m_DesignerNotes.ToLowerInvariant().Contains(needle);
        #endregion
    }
}
