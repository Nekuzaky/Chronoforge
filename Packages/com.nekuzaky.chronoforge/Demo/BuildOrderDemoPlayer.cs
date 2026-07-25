using UnityEngine;

namespace Chronoforge.Demo
{
    /// <summary>
    /// Plays a <see cref="BuildOrderAsset"/> back in Play mode: advances a clock, highlights the
    /// current step, and draws a lightweight overlay to scrub/pause/adjust speed. Proves the
    /// runtime layer (simulation) is usable at runtime — the seed of a future in-game overlay.
    /// Update-driven; no coroutines.
    /// </summary>
    [AddComponentMenu("Chronoforge/Build Order Demo Player")]
    public sealed class BuildOrderDemoPlayer : MonoBehaviour
    {
        public BuildOrderAsset m_BuildOrder;
        public bool m_AutoPlay = true;
        [Range(0.25f, 8f)] public float m_Speed = 2f;

        private BuildOrderEvaluationResult _evaluation = new();
        private float _elapsed;
        private bool _playing;
        private Vector2 _scroll;

        private void OnEnable() => Reload();

        private void Reload()
        {
            _evaluation = m_BuildOrder != null ? BuildOrderEvaluation.Run(m_BuildOrder) : new BuildOrderEvaluationResult();
            _elapsed = 0f;
            _playing = m_AutoPlay && m_BuildOrder != null;
        }

        private void Update()
        {
            if (!_playing || m_BuildOrder == null)
                return;

            _elapsed += Time.deltaTime * m_Speed;
            float total = _evaluation.m_TotalSeconds;
            if (total > 0f && _elapsed >= total)
            {
                _elapsed = total;
                _playing = false;
            }
        }

        private int CurrentIndex()
        {
            int index = -1;
            for (int i = 0; i < _evaluation.m_Timeline.Count; i++)
            {
                if (_evaluation.m_Timeline[i].m_StartSeconds <= _elapsed)
                    index = i;
                else
                    break;
            }
            return index;
        }

        #region Overlay
        private void OnGUI()
        {
            if (m_BuildOrder == null)
            {
                GUI.Box(new Rect(16, 16, 360, 60), "Chronoforge Demo\nAssign a Build Order asset to the player.");
                return;
            }

            const float width = 380f;
            GUILayout.BeginArea(new Rect(16, 16, width, Screen.height - 32), GUI.skin.box);

            GUILayout.Label($"<b>{m_BuildOrder.m_Title}</b>", RichLabel());
            GUILayout.Label($"{BuildOrderTime.Format(_elapsed)} / {BuildOrderTime.Format(_evaluation.m_TotalSeconds)}    ·    supply {ProjectedSupply()}");

            DrawTransport();
            GUILayout.Space(6);
            DrawStepList(CurrentIndex());

            GUILayout.EndArea();
        }

        private void DrawTransport()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_playing ? "Pause" : "Play"))
                _playing = !_playing;
            if (GUILayout.Button("Restart"))
            {
                _elapsed = 0f;
                _playing = true;
            }
            GUILayout.Label("Speed", GUILayout.Width(44));
            m_Speed = GUILayout.HorizontalSlider(m_Speed, 0.25f, 8f, GUILayout.Width(90));
            GUILayout.Label($"{m_Speed:0.0}x", GUILayout.Width(34));
            GUILayout.EndHorizontal();

            float total = Mathf.Max(1f, _evaluation.m_TotalSeconds);
            _elapsed = GUILayout.HorizontalSlider(_elapsed, 0f, total);
        }

        private void DrawStepList(int current)
        {
            _scroll = GUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < m_BuildOrder.m_Steps.Count; i++)
            {
                BuildOrderStep step = m_BuildOrder.m_Steps[i];
                bool active = i == current;
                string title = string.IsNullOrWhiteSpace(step.m_Title) ? $"({step.m_Type})" : step.m_Title;
                string line = $"{step.m_Supply,3}  {step.DisplayTime,5}  {title}";
                GUILayout.Label(active ? $"<b>▶ {line}</b>" : $"   {line}", RichLabel());
            }
            GUILayout.EndScrollView();
        }

        private int ProjectedSupply()
        {
            int index = CurrentIndex();
            return index >= 0 ? _evaluation.m_Timeline[index].m_ProjectedSupply : 0;
        }

        private static GUIStyle RichLabel() => new(GUI.skin.label) { richText = true };
        #endregion
    }
}
