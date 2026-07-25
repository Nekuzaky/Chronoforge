using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// The Chronoforge workspace: a three-column layout (list · details · timeline+validation)
    /// driven by a single <see cref="BuildOrderEditorContext"/>. Owns panel wiring, refresh
    /// routing and the keyboard-first shortcut map. Holds no build-order logic of its own.
    /// </summary>
    public sealed class BuildOrderEditorWindow : EditorWindow
    {
        private const string k_StyleSheetPath =
            "Packages/com.nekuzaky.chronoforge/src/Editor/UI/Chronoforge.uss";

        private readonly BuildOrderEditorContext _context = new();

        private BuildOrderListView _listView;
        private BuildOrderDetailsPanel _detailsPanel;
        private BuildOrderValidationPanel _validationPanel;
        private BuildOrderTimelineView _timelineView;
        private BuildOrderBenchmarkPanel _benchmarkPanel;
        private BuildOrderStructurePanel _structurePanel;
        private BuildOrderComparePanel _comparePanel;
        private Label _status;

        #region Entry points
        [MenuItem("Window/Chronoforge/Build Order Editor")]
        public static void Open()
        {
            BuildOrderEditorWindow window = GetWindow<BuildOrderEditorWindow>();
            window.titleContent = new GUIContent("Chronoforge");
            window.minSize = new Vector2(760, 420);
            window.Show();
            if (Selection.activeObject is BuildOrderAsset asset)
                window._context.Load(asset);
        }

        public static void OpenFor(BuildOrderAsset asset)
        {
            Open();
            GetWindow<BuildOrderEditorWindow>()._context.Load(asset);
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not BuildOrderAsset asset)
                return false;
            OpenFor(asset);
            return true;
        }
        #endregion

        #region Lifecycle
        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("cf-root");
            root.focusable = true;

            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StyleSheetPath);
            if (sheet != null)
                root.styleSheets.Add(sheet);

            BuildToolbar(root);
            BuildBody(root);
            BuildStatusBar(root);

            _context.Changed += RefreshAll;
            _context.SelectionChanged += RefreshSelection;
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            RefreshAll();
        }

        private void OnDisable()
        {
            _context.Changed -= RefreshAll;
            _context.SelectionChanged -= RefreshSelection;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is BuildOrderAsset asset && asset != _context.m_Asset)
                _context.Load(asset);
        }
        #endregion

        #region Layout
        private void BuildToolbar(VisualElement root)
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("cf-toolbar");
            toolbar.Add(new BuildOrderQuickAddBar(_context));
            root.Add(toolbar);
        }

        private void BuildBody(VisualElement root)
        {
            var body = new VisualElement();
            body.AddToClassList("cf-body");

            var listColumn = new VisualElement();
            listColumn.AddToClassList("cf-column");
            listColumn.AddToClassList("cf-column--list");
            listColumn.Add(new BuildOrderSearchBar(_context));
            _listView = new BuildOrderListView(_context);
            listColumn.Add(_listView);

            var detailsColumn = new VisualElement();
            detailsColumn.AddToClassList("cf-column");
            detailsColumn.AddToClassList("cf-column--details");
            var detailsHeader = new Label("STEP DETAILS");
            detailsHeader.AddToClassList("cf-section-header");
            detailsColumn.Add(detailsHeader);
            _detailsPanel = new BuildOrderDetailsPanel(_context);
            detailsColumn.Add(_detailsPanel);

            var sideColumn = new VisualElement();
            sideColumn.AddToClassList("cf-column");
            sideColumn.AddToClassList("cf-column--side");
            var sideScroll = new ScrollView(ScrollViewMode.Vertical);
            sideScroll.style.flexGrow = 1;
            _timelineView = new BuildOrderTimelineView(_context);
            sideScroll.Add(_timelineView);
            _benchmarkPanel = new BuildOrderBenchmarkPanel(_context);
            sideScroll.Add(_benchmarkPanel);
            _structurePanel = new BuildOrderStructurePanel(_context);
            sideScroll.Add(_structurePanel);
            _comparePanel = new BuildOrderComparePanel(_context);
            sideScroll.Add(_comparePanel);
            _validationPanel = new BuildOrderValidationPanel(_context);
            sideScroll.Add(_validationPanel);
            sideScroll.Add(new BuildOrderExportPanel(_context));
            sideColumn.Add(sideScroll);

            body.Add(listColumn);
            body.Add(detailsColumn);
            body.Add(sideColumn);
            root.Add(body);
        }

        private void BuildStatusBar(VisualElement root)
        {
            var bar = new VisualElement();
            bar.AddToClassList("cf-status");
            _status = new Label();
            bar.Add(_status);
            root.Add(bar);
        }
        #endregion

        #region Refresh
        private void RefreshAll()
        {
            if (_listView == null)
                return;

            _listView.Refresh();
            _detailsPanel.Rebuild();
            _validationPanel.Rebuild();
            _timelineView.Rebuild();
            _benchmarkPanel.Rebuild();
            _structurePanel.Rebuild();
            _comparePanel.Rebuild();
            UpdateStatus();
        }

        private void RefreshSelection()
        {
            _listView.SyncSelectionFromContext();
            _detailsPanel.Rebuild();
        }

        private void UpdateStatus()
        {
            if (_context.m_Asset == null)
            {
                _status.text = "No build order loaded — select or create a Build Order asset.";
                return;
            }

            BuildOrderEvaluationResult evaluation = _context.m_Evaluation;
            _status.text =
                $"{_context.m_Asset.m_Steps.Count} steps   ·   " +
                $"{BuildOrderTime.Format(evaluation.m_TotalSeconds)} total   ·   " +
                $"supply {evaluation.m_FinalSupply}   ·   " +
                $"{evaluation.ErrorCount} errors, {evaluation.WarningCount} warnings";
        }
        #endregion

        #region Shortcuts
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_context.m_Asset == null || IsEditingText())
                return;

            if (TryQuickAddKey(evt.keyCode))
            {
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Delete)
            {
                _context.RemoveSelected();
                evt.StopPropagation();
            }
            else if (evt.ctrlKey && evt.keyCode == KeyCode.D)
            {
                _context.DuplicateSelected();
                evt.StopPropagation();
            }
        }

        private bool TryQuickAddKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.Alpha1: _context.AddStep(BuildOrderActionType.Unit); return true;
                case KeyCode.Alpha2: _context.AddStep(BuildOrderActionType.Building); return true;
                case KeyCode.Alpha3: _context.AddStep(BuildOrderActionType.Upgrade); return true;
                case KeyCode.Alpha4: _context.AddStep(BuildOrderActionType.Economy); return true;
                case KeyCode.Alpha5: _context.AddStep(BuildOrderActionType.Tech); return true;
                case KeyCode.Alpha6: _context.AddStep(BuildOrderActionType.Note); return true;
                default: return false;
            }
        }

        private bool IsEditingText()
        {
            Focusable focused = rootVisualElement.panel?.focusController?.focusedElement;
            return focused is TextField || (focused is VisualElement element && element.GetType().Name.Contains("Text"));
        }
        #endregion
    }
}
