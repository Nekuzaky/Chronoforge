using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Top toolbar of one-click type chips. Each chip adds a step of its type; the same actions
    /// are bound to keyboard shortcuts by the window for a mouse-free workflow.
    /// </summary>
    public sealed class BuildOrderQuickAddBar : VisualElement
    {
        private readonly BuildOrderEditorContext _context;

        public BuildOrderQuickAddBar(BuildOrderEditorContext context)
        {
            _context = context;
            AddToClassList("cf-quickadd");

            var title = new Label("CHRONOFORGE");
            title.AddToClassList("cf-toolbar__title");
            Add(title);

            AddChip("Unit (1)", BuildOrderActionType.Unit);
            AddChip("Building (2)", BuildOrderActionType.Building);
            AddChip("Upgrade (3)", BuildOrderActionType.Upgrade);
            AddChip("Economy (4)", BuildOrderActionType.Economy);
            AddChip("Tech (5)", BuildOrderActionType.Tech);
            AddChip("Note (6)", BuildOrderActionType.Note);
        }

        private void AddChip(string label, BuildOrderActionType type)
        {
            var button = new Button(() => _context.AddStep(type)) { text = label };
            button.AddToClassList("cf-chip");
            Add(button);
        }
    }
}
