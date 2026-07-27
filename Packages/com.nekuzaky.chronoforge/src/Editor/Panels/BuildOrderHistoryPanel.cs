using UnityEditor;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Snapshot history: lists captured snapshots newest-first with restore and delete. Restore
    /// overwrites the build order (behind a confirm) while preserving the history itself, so the
    /// list is a safe undo net for bold edits.
    /// </summary>
    public sealed class BuildOrderHistoryPanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly VisualElement _body;

        public BuildOrderHistoryPanel(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("HISTORY");
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

            var snapshots = _context.m_Asset.m_Snapshots;
            if (snapshots.Count == 0)
            {
                var empty = new Label("No snapshots yet. Take one from the export panel before a risky edit.");
                empty.AddToClassList("cf-empty__text");
                empty.style.paddingLeft = 8;
                empty.style.paddingTop = 4;
                _body.Add(empty);
                return;
            }

            for (int i = snapshots.Count - 1; i >= 0; i--)
                _body.Add(BuildRow(snapshots[i]));
        }

        private VisualElement BuildRow(BuildOrderSnapshot snapshot)
        {
            var row = new VisualElement();
            row.AddToClassList("cf-field-row");
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;

            var label = new Label(string.IsNullOrEmpty(snapshot.m_Label) ? snapshot.m_TimestampUtc : snapshot.m_Label);
            label.style.flexGrow = 1;
            label.tooltip = snapshot.m_TimestampUtc;
            row.Add(label);

            var restore = new Button(() => Restore(snapshot)) { text = "Restore" };
            restore.AddToClassList("cf-chip");
            row.Add(restore);

            var delete = new Button(() => _context.DeleteSnapshot(snapshot)) { text = "×" };
            delete.style.width = 22;
            row.Add(delete);
            return row;
        }

        private void Restore(BuildOrderSnapshot snapshot)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Chronoforge — Restore snapshot",
                $"Overwrite the current build order with '{snapshot.m_Label}'? This can be undone (Ctrl+Z); history is kept.",
                "Restore",
                "Cancel");
            if (!confirmed)
                return;

            if (!_context.RestoreSnapshot(snapshot, out string error))
                EditorUtility.DisplayDialog("Chronoforge — Restore failed", error, "OK");
        }
    }
}
