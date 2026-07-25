using UnityEngine;

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
    /// In-game companion HUD that shows a build order and tracks progress against a clock. Meant
    /// to be driven by the host game — feed <see cref="CurrentTime"/> (or <see cref="Advance"/>)
    /// from real match time — but can run its own clock for testing. Highlights the current step,
    /// previews the next few, and warns when a due benchmark is missed. Update/OnGUI only; no
    /// coroutines, no scene assets required.
    /// </summary>
    [AddComponentMenu("Chronoforge/Build Order Overlay")]
    public sealed class BuildOrderOverlay : MonoBehaviour
    {
        public BuildOrderAsset m_BuildOrder;
        public bool m_UseInternalClock;
        [Range(0.1f, 8f)] public float m_Speed = 1f;
        public OverlayAnchor m_Anchor = OverlayAnchor.TopRight;
        public Vector2 m_Margin = new(16f, 16f);
        public float m_Width = 300f;
        [Range(1, 6)] public int m_UpcomingCount = 3;

        private static readonly Color k_Panel = new(0.09f, 0.10f, 0.12f, 0.92f);
        private static readonly Color k_Accent = new(0.30f, 0.60f, 1.00f, 1f);
        private static readonly Color k_Warn = new(0.88f, 0.66f, 0.20f, 1f);

        private BuildOrderEvaluationResult _evaluation = new();
        private float _time;
        private Texture2D _pixel;
        private GUIStyle _title;
        private GUIStyle _line;
        private GUIStyle _current;

        /// <summary>Match time in seconds the overlay renders against. Clamped to non-negative.</summary>
        public float CurrentTime
        {
            get => _time;
            set => _time = Mathf.Max(0f, value);
        }

        #region Public API
        public void SetBuildOrder(BuildOrderAsset buildOrder)
        {
            m_BuildOrder = buildOrder;
            RebuildEvaluation();
        }

        public void ResetClock() => _time = 0f;

        public void Advance(float deltaSeconds) => _time = Mathf.Max(0f, _time + deltaSeconds);
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            _pixel = new Texture2D(1, 1);
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
            RebuildEvaluation();
        }

        private void OnDisable()
        {
            if (_pixel != null)
                Destroy(_pixel);
        }

        private void Update()
        {
            if (m_UseInternalClock && m_BuildOrder != null)
                _time += Time.deltaTime * m_Speed;
        }

        private void RebuildEvaluation() =>
            _evaluation = m_BuildOrder != null ? BuildOrderEvaluation.Run(m_BuildOrder) : new BuildOrderEvaluationResult();
        #endregion

        #region Rendering
        private int CurrentIndex()
        {
            int index = -1;
            for (int i = 0; i < _evaluation.m_Timeline.Count; i++)
            {
                if (_evaluation.m_Timeline[i].m_StartSeconds <= _time)
                    index = i;
                else
                    break;
            }
            return index;
        }

        private void OnGUI()
        {
            if (m_BuildOrder == null || m_BuildOrder.m_Steps.Count == 0)
                return;

            EnsureStyles();

            int current = CurrentIndex();
            int upcoming = Mathf.Min(m_UpcomingCount, Mathf.Max(0, m_BuildOrder.m_Steps.Count - current - 1));
            string warning = DueBenchmarkWarning();

            const float pad = 10f;
            float line = 18f;
            float height = 40f + line + upcoming * line + (string.IsNullOrEmpty(warning) ? 0f : line) + pad * 2f;
            Rect rect = AnchoredRect(m_Width, height);

            GUI.color = k_Panel;
            GUI.DrawTexture(rect, _pixel);
            GUI.color = k_Accent;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), _pixel);
            GUI.color = Color.white;

            var inner = new Rect(rect.x + pad + 3f, rect.y + pad, rect.width - pad * 2f - 3f, rect.height - pad * 2f);
            GUILayout.BeginArea(inner);

            GUILayout.Label(m_BuildOrder.m_Title, _title);
            GUILayout.Label($"{BuildOrderTime.Format(_time)} / {BuildOrderTime.Format(_evaluation.m_TotalSeconds)}", _line);

            DrawStep(current, _current);
            for (int i = 1; i <= upcoming; i++)
                DrawStep(current + i, _line);

            if (!string.IsNullOrEmpty(warning))
            {
                GUI.color = k_Warn;
                GUILayout.Label(warning, _line);
                GUI.color = Color.white;
            }

            GUILayout.EndArea();
        }

        private void DrawStep(int index, GUIStyle style)
        {
            if (index < 0 || index >= m_BuildOrder.m_Steps.Count)
                return;

            BuildOrderStep step = m_BuildOrder.m_Steps[index];
            string title = string.IsNullOrWhiteSpace(step.m_Title) ? $"({step.m_Type})" : step.m_Title;
            string marker = style == _current ? "▶" : " ";
            GUILayout.Label($"{marker} {step.DisplayTime,5}  {title}", style);
        }

        private string DueBenchmarkWarning()
        {
            for (int i = 0; i < m_BuildOrder.m_Benchmarks.Count; i++)
            {
                BuildOrderBenchmark benchmark = m_BuildOrder.m_Benchmarks[i];
                if (benchmark.m_AnchorTimeSeconds > _time)
                    continue;

                foreach (BuildOrderBenchmarkResult result in _evaluation.m_BenchmarkResults)
                {
                    if (result.m_BenchmarkIndex == i && !result.m_Passed)
                        return $"⚠ behind: {(string.IsNullOrEmpty(benchmark.m_Label) ? benchmark.DisplayAnchor : benchmark.m_Label)}";
                }
            }
            return "";
        }

        private Rect AnchoredRect(float width, float height)
        {
            bool right = m_Anchor is OverlayAnchor.TopRight or OverlayAnchor.BottomRight;
            bool bottom = m_Anchor is OverlayAnchor.BottomLeft or OverlayAnchor.BottomRight;
            float x = right ? Screen.width - width - m_Margin.x : m_Margin.x;
            float y = bottom ? Screen.height - height - m_Margin.y : m_Margin.y;
            return new Rect(x, y, width, height);
        }

        private void EnsureStyles()
        {
            if (_title != null)
                return;

            _title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, richText = true };
            _line = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            _current = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
        }
        #endregion
    }
}
