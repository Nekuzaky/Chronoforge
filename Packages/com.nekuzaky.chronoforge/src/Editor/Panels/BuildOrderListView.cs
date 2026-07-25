using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// The step list: one dense row per step with supply, time, a type badge, the title and a
    /// validation dot. Reorderable by drag when unfiltered; selection is mirrored into the
    /// shared context so the details and timeline panels follow.
    /// </summary>
    public sealed class BuildOrderListView : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly ListView _list;
        private List<BuildOrderStep> _source = new();

        public BuildOrderListView(BuildOrderEditorContext context)
        {
            _context = context;
            style.flexGrow = 1;

            _list = new ListView
            {
                fixedItemHeight = 26,
                selectionType = SelectionType.Multiple,
                showBorder = false,
                makeItem = MakeRow,
                bindItem = BindRow
            };
            _list.style.flexGrow = 1;
            _list.selectionChanged += OnSelectionChanged;
            _list.itemIndexChanged += OnItemIndexChanged;
            Add(_list);

            Refresh();
        }

        public void Refresh()
        {
            _source = _context.GetVisibleSteps();
            _list.itemsSource = _source;
            _list.reorderable = !_context.HasFilter;
            _list.reorderMode = ListViewReorderMode.Animated;
            _list.Rebuild();
            SyncSelectionFromContext();
        }

        public void SyncSelectionFromContext()
        {
            List<BuildOrderStep> selected = _context.GetSelectedSteps();
            var indices = new List<int>(selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                int index = _source.IndexOf(selected[i]);
                if (index >= 0)
                    indices.Add(index);
            }

            if (indices.Count > 0)
                _list.SetSelectionWithoutNotify(indices);
        }

        #region Row rendering
        private static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("cf-row");

            var supply = new Label { name = "supply" };
            supply.AddToClassList("cf-row__supply");
            var time = new Label { name = "time" };
            time.AddToClassList("cf-row__time");
            var badge = new VisualElement { name = "badge" };
            badge.AddToClassList("cf-row__badge");
            var title = new Label { name = "title" };
            title.AddToClassList("cf-row__title");
            var state = new VisualElement { name = "state" };
            state.AddToClassList("cf-row__state");

            row.Add(supply);
            row.Add(time);
            row.Add(badge);
            row.Add(title);
            row.Add(state);
            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            if (index < 0 || index >= _source.Count)
                return;

            BuildOrderStep step = _source[index];
            element.Q<Label>("supply").text = step.m_Supply.ToString();
            element.Q<Label>("time").text = step.DisplayTime;
            element.Q("badge").style.backgroundColor = BuildOrderPalette.TypeColor(step.m_Type);

            var title = element.Q<Label>("title");
            bool empty = string.IsNullOrWhiteSpace(step.m_Title);
            title.text = empty ? $"({step.m_Type})" : step.m_Title;
            title.EnableInClassList("cf-row__title--empty", empty);

            var state = element.Q("state");
            state.EnableInClassList("cf-state--warning", step.m_ValidationState == BuildOrderValidationState.Warning);
            state.EnableInClassList("cf-state--error", step.m_ValidationState == BuildOrderValidationState.Error);
        }
        #endregion

        #region Interaction
        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            var ids = new List<string>();
            foreach (object item in selection)
            {
                if (item is BuildOrderStep step)
                    ids.Add(step.m_Id);
            }
            _context.SetSelection(ids);
        }

        private void OnItemIndexChanged(int from, int to)
        {
            // The ListView already moved the item in the live list. Briefly revert so we can
            // record the pre-move state for Undo, then reapply — making the reorder one undo step.
            var steps = _context.m_Asset.m_Steps;
            if (from < 0 || to < 0 || from >= steps.Count || to >= steps.Count)
            {
                _context.NotifyReordered();
                return;
            }

            BuildOrderStep moved = steps[to];
            steps.RemoveAt(to);
            steps.Insert(from, moved);
            _context.RecordUndo("Reorder Steps");
            steps.RemoveAt(from);
            steps.Insert(to, moved);
            _context.NotifyReordered();
        }
        #endregion
    }
}
