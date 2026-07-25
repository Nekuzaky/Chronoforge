using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Overlay
{
    public enum OverlayAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// In-game companion HUD, rendered with runtime UI Toolkit. Meant to be driven by the host
    /// game — feed <see cref="CurrentTime"/> or call <see cref="Advance"/> from real match time —
    /// but can run its own clock for testing. Highlights the current step, previews the next few,
    /// and warns when a due benchmark is missed.
    /// <para>
    /// The visual tree is built once in C# with inline styles, so the component needs no USS,
    /// prefab or scene asset — only a <see cref="UIDocument"/> with PanelSettings, which is the
    /// standard runtime UI Toolkit setup. Per-frame work is limited to updating label text; rows
    /// are rebuilt only when the build order or visible window changes (no per-frame allocation).
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Chronoforge/Build Order Overlay")]
    public sealed class BuildOrderOverlay : MonoBehaviour
    {
        [Header("Data")]
        public BuildOrderAsset m_BuildOrder;

        [Header("Clock")]
        public bool m_UseInternalClock;
        [Range(0.1f, 8f)] public float m_Speed = 1f;

        [Header("Layout")]
        public OverlayAnchor m_Anchor = OverlayAnchor.TopRight;
        public Vector2 m_Margin = new(16f, 16f);
        public float m_Width = 300f;
        [Range(1, 6)] public int m_UpcomingCount = 3;

        private const int k_MaxRows = 7;

        private UIDocument _document;
        private VisualElement _panel;
        private Label _title;
        private Label _clock;
        private Label _warning;
        private readonly List<Label> _rows = new(k_MaxRows);

        private BuildOrderEvaluationResult _evaluation = new();
        private float _time;
        private int _renderedIndex = int.MinValue;
        private bool _warnedAboutPanelSettings;

        /// <summary>Match time in seconds the overlay renders against. Never negative.</summary>
        public float CurrentTime
        {
            get => _time;
            set => _time = Mathf.Max(0f, value);
        }

        #region Public API
        /// <summary>Swaps the displayed build order and re-runs the analysis.</summary>
        public void SetBuildOrder(BuildOrderAsset buildOrder)
        {
            m_BuildOrder = buildOrder;
            Reevaluate();
            _renderedIndex = int.MinValue;
            RefreshRows();
        }

        public void ResetClock() => _time = 0f;

        public void Advance(float deltaSeconds) => _time = Mathf.Max(0f, _time + deltaSeconds);
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            Reevaluate();

            if (!TryGetRoot(out VisualElement root))
                return;

            BuildTree(root);
            _renderedIndex = int.MinValue;
            RefreshRows();
        }

        private void OnDisable()
        {
            _panel?.RemoveFromHierarchy();
            _panel = null;
            _rows.Clear();
        }

        private void Update()
        {
            if (m_BuildOrder == null || _panel == null)
                return;

            if (m_UseInternalClock)
                _time += Time.deltaTime * m_Speed;

            UpdateClock();

            int index = BuildOrderTimelineQuery.IndexAt(_evaluation, _time);
            if (index != _renderedIndex)
                RefreshRows();
        }

        private void Reevaluate() =>
            _evaluation = m_BuildOrder != null ? BuildOrderEvaluation.Run(m_BuildOrder) : new BuildOrderEvaluationResult();

        /// <summary>
        /// Resolves the document root, warning once when PanelSettings are missing — without it
        /// runtime UI Toolkit renders nothing, which is otherwise silent and confusing.
        /// </summary>
        private bool TryGetRoot(out VisualElement root)
        {
            root = _document != null ? _document.rootVisualElement : null;
            if (root != null)
                return true;

            if (!_warnedAboutPanelSettings)
            {
                _warnedAboutPanelSettings = true;
                Debug.LogWarning(
                    "[Chronoforge] Overlay needs a UIDocument with a PanelSettings asset assigned. " +
                    "Use Chronoforge ▸ Create Overlay Setup, or assign one manually.",
                    this);
            }
            return false;
        }
        #endregion

        #region Tree construction
        private void BuildTree(VisualElement root)
        {
            _panel = new VisualElement { name = "chronoforge-overlay", pickingMode = PickingMode.Ignore };
            ApplyPanelStyle(_panel.style);
            ApplyAnchor(_panel.style);

            _title = MakeLabel(13, FontStyle.Bold, BuildOrderColors.k_Text);
            _title.text = m_BuildOrder != null ? m_BuildOrder.m_Title : "No build order";
            _panel.Add(_title);

            _clock = MakeLabel(11, FontStyle.Normal, BuildOrderColors.k_Muted);
            _panel.Add(_clock);

            for (int i = 0; i < k_MaxRows; i++)
            {
                Label row = MakeLabel(11, FontStyle.Normal, BuildOrderColors.k_Text);
                row.style.display = DisplayStyle.None;
                _rows.Add(row);
                _panel.Add(row);
            }

            _warning = MakeLabel(11, FontStyle.Bold, BuildOrderColors.k_Warning);
            _warning.style.display = DisplayStyle.None;
            _panel.Add(_warning);

            root.Add(_panel);
        }

        private void ApplyPanelStyle(IStyle style)
        {
            style.position = Position.Absolute;
            style.width = m_Width;
            style.paddingTop = 8;
            style.paddingBottom = 8;
            style.paddingLeft = 10;
            style.paddingRight = 10;
            style.backgroundColor = BuildOrderColors.k_Panel;
            style.borderLeftWidth = 3;
            style.borderLeftColor = BuildOrderColors.k_Accent;
            style.borderTopLeftRadius = 4;
            style.borderBottomLeftRadius = 4;
            style.borderTopRightRadius = 4;
            style.borderBottomRightRadius = 4;
        }

        private void ApplyAnchor(IStyle style)
        {
            bool right = m_Anchor is OverlayAnchor.TopRight or OverlayAnchor.BottomRight;
            bool bottom = m_Anchor is OverlayAnchor.BottomLeft or OverlayAnchor.BottomRight;

            if (right)
                style.right = m_Margin.x;
            else
                style.left = m_Margin.x;

            if (bottom)
                style.bottom = m_Margin.y;
            else
                style.top = m_Margin.y;
        }

        private static Label MakeLabel(int fontSize, FontStyle fontStyle, Color color)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = fontStyle;
            label.style.color = color;
            label.style.marginBottom = 1;
            return label;
        }
        #endregion

        #region Refresh
        private void UpdateClock()
        {
            int supply = BuildOrderTimelineQuery.SupplyAt(_evaluation, _time);
            _clock.text = $"{BuildOrderTime.Format(_time)} / {BuildOrderTime.Format(_evaluation.m_TotalSeconds)}   ·   supply {supply}";
        }

        /// <summary>
        /// Repoints the fixed pool of row labels at the current window of steps. Rows are reused,
        /// never recreated, so a long match allocates nothing here.
        /// </summary>
        private void RefreshRows()
        {
            if (m_BuildOrder == null)
                return;

            int current = BuildOrderTimelineQuery.IndexAt(_evaluation, _time);
            _renderedIndex = current;

            int visible = Mathf.Min(k_MaxRows, 1 + Mathf.Max(0, m_UpcomingCount));
            int first = Mathf.Max(0, current);

            for (int row = 0; row < _rows.Count; row++)
            {
                int stepIndex = first + row;
                bool show = row < visible && stepIndex < m_BuildOrder.m_Steps.Count && current >= 0;
                Label label = _rows[row];

                if (!show)
                {
                    label.style.display = DisplayStyle.None;
                    continue;
                }

                BuildOrderStep step = m_BuildOrder.m_Steps[stepIndex];
                bool isCurrent = stepIndex == current;
                label.style.display = DisplayStyle.Flex;
                label.text = $"{(isCurrent ? "▶" : " ")} {step.DisplayTime,5}  {Title(step)}";
                label.style.color = isCurrent ? BuildOrderColors.k_Text : BuildOrderColors.k_Muted;
                label.style.unityFontStyleAndWeight = isCurrent ? FontStyle.Bold : FontStyle.Normal;
            }

            UpdateWarning();
        }

        private void UpdateWarning()
        {
            string warning = DueBenchmarkWarning();
            _warning.style.display = string.IsNullOrEmpty(warning) ? DisplayStyle.None : DisplayStyle.Flex;
            _warning.text = warning;
        }

        /// <summary>First benchmark already due that the build fails, or an empty string.</summary>
        private string DueBenchmarkWarning()
        {
            for (int i = 0; i < m_BuildOrder.m_Benchmarks.Count; i++)
            {
                BuildOrderBenchmark benchmark = m_BuildOrder.m_Benchmarks[i];
                if (benchmark.m_AnchorTimeSeconds > _time)
                    continue;

                for (int r = 0; r < _evaluation.m_BenchmarkResults.Count; r++)
                {
                    BuildOrderBenchmarkResult result = _evaluation.m_BenchmarkResults[r];
                    if (result.m_BenchmarkIndex != i || result.m_Passed)
                        continue;

                    string name = string.IsNullOrEmpty(benchmark.m_Label) ? benchmark.DisplayAnchor : benchmark.m_Label;
                    return $"behind: {name}";
                }
            }
            return "";
        }

        private static string Title(BuildOrderStep step) =>
            string.IsNullOrWhiteSpace(step.m_Title) ? $"({step.m_Type})" : step.m_Title;
        #endregion
    }
}
