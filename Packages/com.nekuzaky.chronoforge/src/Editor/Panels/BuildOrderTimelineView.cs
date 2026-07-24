using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Horizontal timeline of the simulated build order. Each step is a bar positioned by start
    /// time and sized by estimated duration; shortfall steps turn red. Benchmark checkpoints are
    /// drawn as vertical markers over the bars, coloured by pass/fail. Clicking a bar selects the
    /// step. Purely a view over the evaluation result.
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

            var track = new VisualElement();
            track.style.position = Position.Relative;

            foreach (BuildOrderTimelineEntry entry in evaluation.m_Timeline)
                track.Add(BuildBar(entry, total));

            AddBenchmarkMarkers(track, total);
            _body.Add(track);
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

        private void AddBenchmarkMarkers(VisualElement track, float total)
        {
            var benchmarks = _context.m_Asset.m_Benchmarks;
            for (int i = 0; i < benchmarks.Count; i++)
            {
                BuildOrderBenchmark benchmark = benchmarks[i];
                var marker = new VisualElement();
                marker.AddToClassList("cf-timeline__marker");

                bool passed = ResultPassed(i, out bool hasResult);
                if (hasResult)
                    marker.EnableInClassList(passed ? "cf-timeline__marker--pass" : "cf-timeline__marker--fail", enable: true);

                float leftPercent = Mathf.Clamp(benchmark.m_AnchorTimeSeconds / total * 100f, 0f, 100f);
                marker.style.left = Length.Percent(leftPercent);
                marker.tooltip = $"{(string.IsNullOrEmpty(benchmark.m_Label) ? "Benchmark" : benchmark.m_Label)}  ({benchmark.DisplayAnchor})";
                track.Add(marker);
            }
        }

        private bool ResultPassed(int index, out bool hasResult)
        {
            foreach (BuildOrderBenchmarkResult result in _context.m_Evaluation.m_BenchmarkResults)
            {
                if (result.m_BenchmarkIndex == index)
                {
                    hasResult = true;
                    return result.m_Passed;
                }
            }
            hasResult = false;
            return true;
        }
    }
}
