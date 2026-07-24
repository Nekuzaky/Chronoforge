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

        private string _selectedId = "";
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

        public BuildOrderStep SelectedStep => m_Asset != null ? m_Asset.FindStep(_selectedId) : null;

        public int SelectedIndex => m_Asset != null ? m_Asset.m_Steps.FindIndex(step => step.m_Id == _selectedId) : -1;

        public void Select(string stepId)
        {
            if (_selectedId == stepId)
                return;
            _selectedId = stepId ?? "";
            SelectionChanged?.Invoke();
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
            _selectedId = asset != null && asset.m_Steps.Count > 0 ? asset.m_Steps[0].m_Id : "";
            Reevaluate();
            RaiseChanged();
        }
        #endregion

        #region Mutations
        public void AddStep(BuildOrderActionType type)
        {
            if (m_Asset == null)
                return;

            RecordUndo("Add Build Order Step");
            BuildOrderStep template = m_Asset.m_Steps.Count > 0 ? m_Asset.m_Steps[^1] : null;
            BuildOrderStep step = BuildOrderStep.Create(type);
            if (template != null)
            {
                step.m_Supply = template.m_Supply + 1;
                step.m_TimeSeconds = template.m_TimeSeconds;
                step.m_BranchKey = template.m_BranchKey;
            }
            m_Asset.m_Steps.Add(step);
            _selectedId = step.m_Id;
            RaiseChanged();
        }

        public void DuplicateSelected()
        {
            int index = SelectedIndex;
            if (index < 0)
                return;

            RecordUndo("Duplicate Build Order Step");
            BuildOrderStep clone = m_Asset.m_Steps[index].Clone();
            m_Asset.m_Steps.Insert(index + 1, clone);
            _selectedId = clone.m_Id;
            RaiseChanged();
        }

        public void RemoveSelected()
        {
            int index = SelectedIndex;
            if (index < 0)
                return;

            RecordUndo("Delete Build Order Step");
            m_Asset.m_Steps.RemoveAt(index);
            int next = Mathf.Clamp(index, 0, m_Asset.m_Steps.Count - 1);
            _selectedId = m_Asset.m_Steps.Count > 0 ? m_Asset.m_Steps[next].m_Id : "";
            RaiseChanged();
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
