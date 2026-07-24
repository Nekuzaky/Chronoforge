using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>Filter box above the step list. Writes straight into the context's search state.</summary>
    public sealed class BuildOrderSearchBar : VisualElement
    {
        public BuildOrderSearchBar(BuildOrderEditorContext context)
        {
            AddToClassList("cf-searchbar");

            var field = new ToolbarSearchField { value = context.Search };
            field.RegisterValueChangedCallback(evt => context.Search = evt.newValue);
            Add(field);
        }
    }
}
