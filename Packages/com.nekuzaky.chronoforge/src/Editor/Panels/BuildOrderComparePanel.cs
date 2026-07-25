using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Planned-vs-actual comparison. Picks a snapshot as the baseline (planned) and diffs it
    /// against the current build (actual), listing additions, removals and time/supply/type
    /// shifts. Only differences are shown, to stay signal-dense. Rows jump to their step.
    /// </summary>
    public sealed class BuildOrderComparePanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly VisualElement _body;
        private int _snapshotIndex;

        private BuildOrderAsset _cachedBaseline;
        private string _cachedId = "";

        public BuildOrderComparePanel(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("COMPARE");
            header.AddToClassList("cf-section-header");
            Add(header);

            _body = new VisualElement();
            Add(_body);

            RegisterCallback<DetachFromPanelEvent>(_ => ReleaseBaseline());
            Rebuild();
        }

        public void Rebuild()
        {
            _body.Clear();
            if (_context.m_Asset == null)
                return;

            List<BuildOrderSnapshot> snapshots = _context.m_Asset.m_Snapshots;
            if (snapshots.Count == 0)
            {
                var empty = new Label("Take a snapshot, then compare the current build against it.");
                empty.AddToClassList("cf-empty__text");
                empty.style.paddingLeft = 8;
                empty.style.paddingTop = 4;
                _body.Add(empty);
                return;
            }

            _snapshotIndex = Mathf.Clamp(_snapshotIndex, 0, snapshots.Count - 1);

            var labels = new List<string>();
            foreach (BuildOrderSnapshot snapshot in snapshots)
                labels.Add(string.IsNullOrEmpty(snapshot.m_Label) ? snapshot.m_TimestampUtc : snapshot.m_Label);

            var dropdown = new DropdownField("Baseline", labels, _snapshotIndex);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                _snapshotIndex = labels.IndexOf(evt.newValue);
                Rebuild();
            });
            _body.Add(dropdown);

            RenderDiff(snapshots[_snapshotIndex]);
        }

        private void RenderDiff(BuildOrderSnapshot snapshot)
        {
            BuildOrderAsset baseline = GetBaseline(snapshot, out string error);
            if (baseline == null)
            {
                var failed = new Label($"Snapshot unreadable: {error}");
                failed.AddToClassList("cf-empty__text");
                _body.Add(failed);
                return;
            }

            BuildOrderDiff diff = BuildOrderComparer.Compare(baseline, _context.m_Asset);

            var summary = new Label($"+{diff.m_Added} added   ·   -{diff.m_Removed} removed   ·   ~{diff.m_Changed} changed");
            summary.AddToClassList("cf-benchmark__detail");
            summary.style.paddingLeft = 8;
            _body.Add(summary);

            if (!diff.HasChanges)
            {
                var clean = new Label("Identical to baseline.");
                clean.AddToClassList("cf-empty__text");
                clean.style.paddingLeft = 8;
                _body.Add(clean);
                return;
            }

            foreach (BuildOrderDiffEntry entry in diff.m_Entries)
            {
                if (entry.m_ChangeType == BuildOrderChangeType.Unchanged)
                    continue;
                _body.Add(BuildDiffRow(entry));
            }
        }

        /// <summary>
        /// Rehydrates the snapshot into a detached asset, cached by snapshot id so repeated
        /// refreshes don't re-parse. The cached instance is released on id change or panel detach.
        /// </summary>
        private BuildOrderAsset GetBaseline(BuildOrderSnapshot snapshot, out string error)
        {
            error = "";
            string id = string.IsNullOrEmpty(snapshot.m_Id) ? snapshot.m_TimestampUtc : snapshot.m_Id;
            if (_cachedBaseline != null && _cachedId == id)
                return _cachedBaseline;

            ReleaseBaseline();
            _cachedBaseline = BuildOrderSerializer.CreateFromJson(snapshot.m_Json, out error);
            _cachedId = _cachedBaseline != null ? id : "";
            return _cachedBaseline;
        }

        private void ReleaseBaseline()
        {
            if (_cachedBaseline != null)
                Object.DestroyImmediate(_cachedBaseline);
            _cachedBaseline = null;
            _cachedId = "";
        }

        private VisualElement BuildDiffRow(BuildOrderDiffEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("cf-diff");
            row.AddToClassList(ChangeClass(entry.m_ChangeType));

            var glyph = new Label(Glyph(entry.m_ChangeType));
            glyph.AddToClassList("cf-diff__glyph");
            row.Add(glyph);

            var text = new Label($"{entry.m_Title} — {entry.m_Detail}");
            text.AddToClassList("cf-diff__text");
            row.Add(text);

            if (!string.IsNullOrEmpty(entry.m_StepId))
                row.RegisterCallback<ClickEvent>(_ => _context.Select(entry.m_StepId));
            return row;
        }

        private static string Glyph(BuildOrderChangeType type) => type switch
        {
            BuildOrderChangeType.Added => "+",
            BuildOrderChangeType.Removed => "−",
            _ => "~"
        };

        private static string ChangeClass(BuildOrderChangeType type) => type switch
        {
            BuildOrderChangeType.Added => "cf-diff--added",
            BuildOrderChangeType.Removed => "cf-diff--removed",
            _ => "cf-diff--changed"
        };
    }
}
