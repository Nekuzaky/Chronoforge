using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Filter bar above the step list: free-text search plus one toggle chip per tag, tinted with
    /// the tag's own colour. Tags combine as "any of" so the chips narrow by facet, and a Clear
    /// affordance appears only while something is filtered.
    /// </summary>
    public sealed class BuildOrderSearchBar : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly VisualElement _tagRow;

        public BuildOrderSearchBar(BuildOrderEditorContext context)
        {
            _context = context;
            style.flexDirection = FlexDirection.Column;

            var searchRow = new VisualElement();
            searchRow.AddToClassList("cf-searchbar");

            var field = new ToolbarSearchField { value = context.Search };
            field.RegisterValueChangedCallback(evt => context.Search = evt.newValue);
            searchRow.Add(field);
            Add(searchRow);

            _tagRow = new VisualElement();
            _tagRow.AddToClassList("cf-tagfilter");
            Add(_tagRow);

            Rebuild();
        }

        public void Rebuild()
        {
            _tagRow.Clear();
            if (_context.m_Asset == null || _context.m_Asset.m_Tags.Count == 0)
                return;

            foreach (BuildOrderTag tag in _context.m_Asset.m_Tags)
                _tagRow.Add(BuildChip(tag));

            if (_context.HasFilter)
                _tagRow.Add(BuildClearButton());
        }

        private Button BuildChip(BuildOrderTag tag)
        {
            bool active = _context.IsTagFiltered(tag.m_Id);

            var chip = new Button(() => _context.ToggleTagFilter(tag.m_Id))
            {
                text = string.IsNullOrEmpty(tag.m_Label) ? "(tag)" : tag.m_Label
            };
            chip.AddToClassList("cf-chip");
            chip.AddToClassList("cf-chip--tag");

            if (active && ColorUtility.TryParseHtmlString(tag.m_ColorHex, out Color color))
            {
                chip.style.backgroundColor = color;
                chip.style.color = Contrast(color);
            }
            return chip;
        }

        private Button BuildClearButton()
        {
            var clear = new Button(() => _context.ClearFilters()) { text = "Clear" };
            clear.AddToClassList("cf-chip");
            clear.AddToClassList("cf-chip--clear");
            return clear;
        }

        /// <summary>Picks readable text for a chip filled with the tag's colour.</summary>
        private static Color Contrast(Color background)
        {
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return luminance > 0.55f ? new Color(0.07f, 0.08f, 0.09f) : Color.white;
        }
    }
}
