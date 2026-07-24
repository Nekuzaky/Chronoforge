using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Horizontal timeline of the simulated build order. Each step is a bar positioned by start
    /// time and sized by estimated duration; shortfall steps turn red. Clicking a bar selects
    /// the step. Purely a view over <see cref="BuildOrderEvaluationResult"/>.
    /// </summary>
    public sealed class BuildOrderTimelineView : VisualElement
    {
        private const float k_MinBarWidth = 6f;
        private readonly BuildOrderEditorContext _context;
        private readonly VisualElement _body;

        public BuildOrderTimelineView(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("TIMELINE");
            header.AddToClassList("cf-section-header");
            Add(header);

            _body = new VisualElement();
            _body.AddToClassList("cf-timeline");
            Add(_body);

            Rebuild();
        }

        public void Rebuild()
        {
            _body.Clear();

            BuildOrderEvaluationResult evaluation = _context.m_Evaluation;
            if (evaluation.m_Timeline.Count == 0)
            {
                var empty = new Label("Timeline appears once steps have times.");
                empty.AddToClassList("cf-empty__text");
                _body.Add(empty);
                return;
            }

            float total = Mathf.Max(1f, evaluation.m_TotalSeconds);
            foreach (BuildOrderTimelineEntry entry in evaluation.m_Timeline)
                _body.Add(BuildBar(entry, total));
        }

        private VisualElement BuildBar(BuildOrderTimelineEntry entry, float total)
        {
            BuildOrderStep step = _context.m_Asset.FindStep(entry.m_StepId);

            var bar = new VisualElement();
            bar.AddToClassList("cf-timeline__bar");
            if (entry.m_HasResourceShortfall)
                bar.AddToClassList("cf-timeline__bar--shortfall");
            else if (step != null)
                bar.style.backgroundColor = BuildOrderPalette.TypeColor(step.m_Type);

            float leftPercent = entry.m_StartSeconds / total * 100f;
            float widthPercent = Mathf.Max(entry.Duration / total * 100f, k_MinBarWidth);
            bar.style.marginLeft = Length.Percent(Mathf.Clamp(leftPercent, 0f, 95f));
            bar.style.width = Length.Percent(Mathf.Clamp(widthPercent, k_MinBarWidth, 100f));
            bar.tooltip = step != null ? $"{step.m_Title}  ({step.DisplayTime})" : entry.m_StepId;

            bar.RegisterCallback<ClickEvent>(_ => _context.Select(entry.m_StepId));
            return bar;
        }
    }
}
