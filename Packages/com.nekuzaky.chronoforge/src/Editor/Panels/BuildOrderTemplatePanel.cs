using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Templates: reuse a sequence as a starting point. Any <see cref="BuildOrderAsset"/> flagged
    /// as a template shows up here — no separate asset type to manage — and can be appended to or
    /// replace the current steps. The current asset can also be promoted to a template in place.
    /// </summary>
    public sealed class BuildOrderTemplatePanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly VisualElement _body;

        public BuildOrderTemplatePanel(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("TEMPLATES");
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

            var isTemplate = new Toggle("This is a template") { value = _context.m_Asset.m_IsTemplate };
            isTemplate.tooltip = "Templates appear in this list for every other build order.";
            isTemplate.RegisterValueChangedCallback(evt =>
            {
                _context.RecordUndo("Toggle Template");
                _context.m_Asset.m_IsTemplate = evt.newValue;
                _context.NotifyChanged();
            });
            _body.Add(isTemplate);

            List<BuildOrderAsset> templates = FindTemplates();
            if (templates.Count == 0)
            {
                var empty = new Label("No templates yet. Flag a build order as a template to reuse its sequence.");
                empty.AddToClassList("cf-empty__text");
                empty.style.paddingLeft = 8;
                empty.style.whiteSpace = WhiteSpace.Normal;
                _body.Add(empty);
                return;
            }

            for (int i = 0; i < templates.Count; i++)
                _body.Add(BuildRow(templates[i]));
        }

        private VisualElement BuildRow(BuildOrderAsset template)
        {
            var row = new VisualElement();
            row.AddToClassList("cf-field-row");
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;

            var label = new Label($"{template.m_Title}  ({template.m_Steps.Count})");
            label.style.flexGrow = 1;
            label.style.overflow = Overflow.Hidden;
            label.tooltip = AssetDatabase.GetAssetPath(template);
            row.Add(label);

            var append = new Button(() => Apply(template, replace: false)) { text = "Append" };
            append.AddToClassList("cf-chip");
            row.Add(append);

            var replace = new Button(() => Apply(template, replace: true)) { text = "Replace" };
            replace.AddToClassList("cf-chip");
            row.Add(replace);
            return row;
        }

        private void Apply(BuildOrderAsset template, bool replace)
        {
            if (replace && _context.m_Asset.m_Steps.Count > 0)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Chronoforge — Apply template",
                    $"Replace all {_context.m_Asset.m_Steps.Count} steps with '{template.m_Title}'? This can be undone (Ctrl+Z).",
                    "Replace",
                    "Cancel");
                if (!confirmed)
                    return;
            }

            _context.ApplyTemplate(template, replace);
        }

        /// <summary>All template assets in the project except the one being edited.</summary>
        private List<BuildOrderAsset> FindTemplates()
        {
            var templates = new List<BuildOrderAsset>();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BuildOrderAsset)}");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<BuildOrderAsset>(path);
                if (asset != null && asset.m_IsTemplate && asset != _context.m_Asset)
                    templates.Add(asset);
            }
            return templates;
        }
    }
}
