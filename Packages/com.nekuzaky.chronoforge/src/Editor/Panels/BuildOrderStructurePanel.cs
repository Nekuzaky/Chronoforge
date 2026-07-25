using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Author asset-level structure: conditional branches (key, name, colour, gating condition)
    /// and tag definitions. Steps reference these by key/id elsewhere; this panel is where they
    /// are created and named.
    /// </summary>
    public sealed class BuildOrderStructurePanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly VisualElement _body;

        public BuildOrderStructurePanel(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("BRANCHES & TAGS");
            header.AddToClassList("cf-section-header");
            Add(header);

            _body = new VisualElement();
            Add(_body);

            Rebuild();
        }

        public void Rebuild()
        {
            _body.Clear();
            if (_context.m_Asset == null)
                return;

            BuildBranches();
            BuildTags();
        }

        #region Branches
        private void BuildBranches()
        {
            var sub = new Label("Branches");
            sub.AddToClassList("cf-subheader");
            sub.style.paddingLeft = 8;
            _body.Add(sub);

            foreach (BuildOrderBranch branch in _context.m_Asset.m_Branches)
                _body.Add(BuildBranchCard(branch));

            _body.Add(AddButton("+ Branch", () =>
            {
                _context.RecordUndo("Add Branch");
                _context.m_Asset.m_Branches.Add(new BuildOrderBranch { m_Key = $"branch{_context.m_Asset.m_Branches.Count + 1}" });
                _context.NotifyChanged();
            }));
        }

        private VisualElement BuildBranchCard(BuildOrderBranch branch)
        {
            var card = new VisualElement();
            card.AddToClassList("cf-benchmark");

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.alignItems = Align.Center;

            var color = new ColorField { value = HexToColor(branch.m_ColorHex), showAlpha = false };
            color.style.width = 40;
            color.RegisterValueChangedCallback(evt => Commit(() => branch.m_ColorHex = ColorToHex(evt.newValue), "Edit Branch Colour"));
            head.Add(color);

            var key = new TextField { value = branch.m_Key, isDelayed = true, tooltip = "Branch key (referenced by steps)" };
            key.style.flexGrow = 1;
            key.RegisterValueChangedCallback(evt => Commit(() => branch.m_Key = evt.newValue, "Edit Branch Key"));
            head.Add(key);

            var remove = new Button(() =>
            {
                _context.RecordUndo("Remove Branch");
                _context.m_Asset.m_Branches.Remove(branch);
                _context.NotifyChanged();
            }) { text = "×" };
            remove.style.width = 22;
            head.Add(remove);
            card.Add(head);

            var name = new TextField("Name") { value = branch.m_Name, isDelayed = true };
            name.RegisterValueChangedCallback(evt => Commit(() => branch.m_Name = evt.newValue, "Edit Branch Name"));
            card.Add(name);

            card.Add(BuildConditionRow(branch.m_Condition));
            return card;
        }

        private VisualElement BuildConditionRow(BuildOrderCondition condition)
        {
            var row = new VisualElement();
            row.AddToClassList("cf-field-row");

            var variable = new TextField { value = condition.m_Variable, isDelayed = true, tooltip = "Condition variable" };
            variable.style.flexGrow = 1;
            variable.RegisterValueChangedCallback(evt => Commit(() => condition.m_Variable = evt.newValue, "Edit Condition Variable"));
            row.Add(variable);

            var op = new EnumField(condition.m_Operator);
            op.style.width = 64;
            op.RegisterValueChangedCallback(evt => Commit(() => condition.m_Operator = (BuildOrderConditionOperator)evt.newValue, "Edit Condition Operator"));
            row.Add(op);

            var value = new TextField { value = condition.m_Value, isDelayed = true, tooltip = "Condition value" };
            value.style.width = 56;
            value.RegisterValueChangedCallback(evt => Commit(() => condition.m_Value = evt.newValue, "Edit Condition Value"));
            row.Add(value);
            return row;
        }
        #endregion

        #region Tags
        private void BuildTags()
        {
            var sub = new Label("Tags");
            sub.AddToClassList("cf-subheader");
            sub.style.paddingLeft = 8;
            _body.Add(sub);

            foreach (BuildOrderTag tag in _context.m_Asset.m_Tags)
                _body.Add(BuildTagRow(tag));

            _body.Add(AddButton("+ Tag", () =>
            {
                _context.RecordUndo("Add Tag");
                _context.m_Asset.m_Tags.Add(new BuildOrderTag
                {
                    m_Id = Guid.NewGuid().ToString("N"),
                    m_Label = $"tag{_context.m_Asset.m_Tags.Count + 1}"
                });
                _context.NotifyChanged();
            }));
        }

        private VisualElement BuildTagRow(BuildOrderTag tag)
        {
            var row = new VisualElement();
            row.AddToClassList("cf-field-row");
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;

            var color = new ColorField { value = HexToColor(tag.m_ColorHex), showAlpha = false };
            color.style.width = 40;
            color.RegisterValueChangedCallback(evt => Commit(() => tag.m_ColorHex = ColorToHex(evt.newValue), "Edit Tag Colour"));
            row.Add(color);

            var label = new TextField { value = tag.m_Label, isDelayed = true };
            label.style.flexGrow = 1;
            label.RegisterValueChangedCallback(evt => Commit(() => tag.m_Label = evt.newValue, "Edit Tag Label"));
            row.Add(label);

            var remove = new Button(() => _context.RemoveTag(tag)) { text = "×" };
            remove.style.width = 22;
            row.Add(remove);
            return row;
        }
        #endregion

        #region Helpers
        private Button AddButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("cf-chip");
            button.style.marginLeft = 8;
            button.style.marginTop = 2;
            button.style.marginBottom = 4;
            return button;
        }

        private void Commit(Action mutation, string undoName)
        {
            _context.RecordUndo(undoName);
            mutation();
            _context.NotifyChanged();
        }

        private static Color HexToColor(string hex) =>
            ColorUtility.TryParseHtmlString(string.IsNullOrEmpty(hex) ? "#8C9BAB" : hex, out Color color) ? color : Color.gray;

        private static string ColorToHex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);
        #endregion
    }
}
