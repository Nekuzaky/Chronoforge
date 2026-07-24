using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Editor for the selected step. Rebuilds on selection change; every field commits through
    /// the context so Undo, dirtying and re-validation happen on each edit. Shows a clean empty
    /// state when nothing is selected.
    /// </summary>
    public sealed class BuildOrderDetailsPanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly ScrollView _body;

        public BuildOrderDetailsPanel(BuildOrderEditorContext context)
        {
            _context = context;
            style.flexGrow = 1;

            _body = new ScrollView(ScrollViewMode.Vertical);
            _body.AddToClassList("cf-details");
            _body.style.flexGrow = 1;
            Add(_body);

            Rebuild();
        }

        public void Rebuild()
        {
            _body.Clear();
            BuildOrderStep step = _context.SelectedStep;
            if (step == null)
            {
                ShowEmptyState();
                return;
            }

            BuildIdentity(step);
            BuildTiming(step);
            BuildResourceCost(step);
            BuildOrganisation(step);
        }

        #region Sections
        private void BuildIdentity(BuildOrderStep step)
        {
            AddSubHeader("Identity");

            var title = new TextField("Title") { value = step.m_Title, isDelayed = true };
            title.RegisterValueChangedCallback(evt => Commit(() => step.m_Title = evt.newValue, "Edit Title"));
            _body.Add(title);

            var type = new EnumField("Type", step.m_Type);
            type.RegisterValueChangedCallback(evt => Commit(() => step.m_Type = (BuildOrderActionType)evt.newValue, "Edit Type"));
            _body.Add(type);

            var description = new TextField("Description") { value = step.m_Description, multiline = true, isDelayed = true };
            description.RegisterValueChangedCallback(evt => Commit(() => step.m_Description = evt.newValue, "Edit Description"));
            _body.Add(description);
        }

        private void BuildTiming(BuildOrderStep step)
        {
            AddSubHeader("Timing & supply");

            var supply = new IntegerField("Supply") { value = step.m_Supply, isDelayed = true };
            supply.RegisterValueChangedCallback(evt => Commit(() => step.m_Supply = evt.newValue, "Edit Supply"));
            _body.Add(supply);

            var time = new TextField("Time (M:SS)") { value = step.DisplayTime };
            time.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (BuildOrderTime.TryParse(time.value, out float seconds))
                    Commit(() => step.m_TimeSeconds = seconds, "Edit Time");
                time.SetValueWithoutNotify(step.DisplayTime);
            });
            _body.Add(time);

            var popReq = new IntegerField("Pop. requirement") { value = step.m_PopulationRequirement, isDelayed = true };
            popReq.RegisterValueChangedCallback(evt => Commit(() => step.m_PopulationRequirement = evt.newValue, "Edit Pop Requirement"));
            _body.Add(popReq);

            var popDelta = new IntegerField("Pop. delta") { value = step.m_PopulationDelta, isDelayed = true };
            popDelta.RegisterValueChangedCallback(evt => Commit(() => step.m_PopulationDelta = evt.newValue, "Edit Pop Delta"));
            _body.Add(popDelta);

            var duration = new FloatField("Est. duration (s)") { value = step.m_EstimatedDuration, isDelayed = true };
            duration.RegisterValueChangedCallback(evt => Commit(() => step.m_EstimatedDuration = evt.newValue, "Edit Duration"));
            _body.Add(duration);
        }

        private void BuildResourceCost(BuildOrderStep step)
        {
            AddSubHeader("Resource cost");
            var container = new VisualElement();
            _body.Add(container);
            RebuildResourceRows(step, container);
        }

        private void RebuildResourceRows(BuildOrderStep step, VisualElement container)
        {
            container.Clear();
            List<BuildOrderResourceAmount> amounts = step.m_ResourceCost.m_Amounts;

            for (int i = 0; i < amounts.Count; i++)
            {
                BuildOrderResourceAmount amount = amounts[i];
                var row = new VisualElement();
                row.AddToClassList("cf-field-row");

                var id = new TextField { value = amount.m_ResourceId, isDelayed = true };
                id.style.flexGrow = 1;
                id.RegisterValueChangedCallback(evt => Commit(() => amount.m_ResourceId = evt.newValue, "Edit Resource"));

                var value = new FloatField { value = amount.m_Amount, isDelayed = true };
                value.style.width = 70;
                value.RegisterValueChangedCallback(evt => Commit(() => amount.m_Amount = evt.newValue, "Edit Cost"));

                var remove = new Button(() =>
                {
                    _context.RecordUndo("Remove Resource");
                    amounts.Remove(amount);
                    _context.NotifyChanged();
                    RebuildResourceRows(step, container);
                }) { text = "×" };
                remove.style.width = 22;

                row.Add(id);
                row.Add(value);
                row.Add(remove);
                container.Add(row);
            }

            var add = new Button(() =>
            {
                _context.RecordUndo("Add Resource");
                amounts.Add(new BuildOrderResourceAmount());
                _context.NotifyChanged();
                RebuildResourceRows(step, container);
            }) { text = "+ Resource" };
            add.AddToClassList("cf-chip");
            container.Add(add);
        }

        private void BuildOrganisation(BuildOrderStep step)
        {
            AddSubHeader("Organisation");

            var branchChoices = new List<string> { "(mainline)" };
            foreach (BuildOrderBranch branch in _context.m_Asset.m_Branches)
                branchChoices.Add(branch.m_Key);

            string current = string.IsNullOrEmpty(step.m_BranchKey) ? "(mainline)" : step.m_BranchKey;
            if (!branchChoices.Contains(current))
                branchChoices.Add(current);

            var branch = new DropdownField("Branch", branchChoices, current);
            branch.RegisterValueChangedCallback(evt =>
                Commit(() => step.m_BranchKey = evt.newValue == "(mainline)" ? "" : evt.newValue, "Edit Branch"));
            _body.Add(branch);

            var priority = new EnumField("Priority", step.m_Priority);
            priority.RegisterValueChangedCallback(evt => Commit(() => step.m_Priority = (BuildOrderPriority)evt.newValue, "Edit Priority"));
            _body.Add(priority);

            var status = new EnumField("Status", step.m_Status);
            status.RegisterValueChangedCallback(evt => Commit(() => step.m_Status = (BuildOrderStepStatus)evt.newValue, "Edit Status"));
            _body.Add(status);

            var optional = new Toggle("Optional") { value = step.m_Optional };
            optional.RegisterValueChangedCallback(evt => Commit(() => step.m_Optional = evt.newValue, "Toggle Optional"));
            _body.Add(optional);

            var repeatable = new Toggle("Repeatable") { value = step.m_Repeatable };
            repeatable.RegisterValueChangedCallback(evt => Commit(() => step.m_Repeatable = evt.newValue, "Toggle Repeatable"));
            _body.Add(repeatable);

            var notes = new TextField("Designer notes") { value = step.m_DesignerNotes, multiline = true, isDelayed = true };
            notes.RegisterValueChangedCallback(evt => Commit(() => step.m_DesignerNotes = evt.newValue, "Edit Notes"));
            _body.Add(notes);
        }
        #endregion

        #region Helpers
        private void ShowEmptyState()
        {
            var empty = new VisualElement();
            empty.AddToClassList("cf-empty");
            var text = new Label("No step selected.\nAdd one from the toolbar or pick a row on the left.");
            text.AddToClassList("cf-empty__text");
            empty.Add(text);
            _body.Add(empty);
        }

        private void AddSubHeader(string text)
        {
            var header = new Label(text);
            header.AddToClassList("cf-subheader");
            _body.Add(header);
        }

        private void Commit(System.Action mutation, string undoName)
        {
            _context.RecordUndo(undoName);
            mutation();
            _context.NotifyChanged();
        }
        #endregion
    }
}
