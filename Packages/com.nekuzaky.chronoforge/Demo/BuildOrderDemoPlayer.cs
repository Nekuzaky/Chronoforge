using Chronoforge.Overlay;
using UnityEngine;

namespace Chronoforge.Demo
{
    /// <summary>
    /// Drives a <see cref="BuildOrderOverlay"/> the way a host game would: it owns the match clock
    /// and pushes it into the overlay, which does all the rendering. Adds a small transport UI
    /// (play / pause / restart / speed / scrub) so the demo is explorable.
    /// <para>
    /// The transport is deliberately drawn with <c>OnGUI</c>: it is a development harness, not
    /// shipping UI, and this keeps the demo free of extra UI assets. The overlay itself — the part
    /// a game would actually ship — is runtime UI Toolkit. Update-driven; no coroutines.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(BuildOrderOverlay))]
    [AddComponentMenu("Chronoforge/Build Order Demo Player")]
    public sealed class BuildOrderDemoPlayer : MonoBehaviour
    {
        public bool m_AutoPlay = true;
        [Range(0.25f, 8f)] public float m_Speed = 2f;
        public bool m_ShowTransport = true;

        private BuildOrderOverlay _overlay;
        private BuildOrderEvaluationResult _evaluation = new();
        private bool _playing;
        private GUIStyle _label;

        private void OnEnable()
        {
            _overlay = GetComponent<BuildOrderOverlay>();

            // The demo owns the clock, so make sure the overlay isn't also advancing it.
            _overlay.m_UseInternalClock = false;
            _overlay.ResetClock();

            _evaluation = _overlay.m_BuildOrder != null
                ? BuildOrderEvaluation.Run(_overlay.m_BuildOrder)
                : new BuildOrderEvaluationResult();

            _playing = m_AutoPlay && _overlay.m_BuildOrder != null;
        }

        private void Update()
        {
            if (!_playing)
                return;

            _overlay.Advance(Time.deltaTime * m_Speed);

            float total = _evaluation.m_TotalSeconds;
            if (total > 0f && _overlay.CurrentTime >= total)
            {
                _overlay.CurrentTime = total;
                _playing = false;
            }
        }

        #region Transport (development harness)
        private void OnGUI()
        {
            if (!m_ShowTransport)
                return;

            EnsureStyles();

            if (_overlay.m_BuildOrder == null)
            {
                GUI.Label(new Rect(16f, 16f, 420f, 22f), "Chronoforge demo: assign a Build Order to the overlay.", _label);
                return;
            }

            GUILayout.BeginArea(new Rect(16f, Screen.height - 74f, 380f, 58f), GUI.skin.box);
            DrawButtons();
            DrawScrubber();
            GUILayout.EndArea();
        }

        private void DrawButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_playing ? "Pause" : "Play"))
                _playing = !_playing;
            if (GUILayout.Button("Restart"))
            {
                _overlay.ResetClock();
                _playing = true;
            }
            GUILayout.Label("Speed", _label, GUILayout.Width(42f));
            m_Speed = GUILayout.HorizontalSlider(m_Speed, 0.25f, 8f, GUILayout.Width(90f));
            GUILayout.Label($"{m_Speed:0.0}x", _label, GUILayout.Width(34f));
            GUILayout.EndHorizontal();
        }

        private void DrawScrubber()
        {
            float total = Mathf.Max(1f, _evaluation.m_TotalSeconds);
            float scrubbed = GUILayout.HorizontalSlider(_overlay.CurrentTime, 0f, total);
            if (!Mathf.Approximately(scrubbed, _overlay.CurrentTime))
                _overlay.CurrentTime = scrubbed;
        }

        private void EnsureStyles() => _label ??= new GUIStyle(GUI.skin.label) { fontSize = 11 };
        #endregion
    }
}
