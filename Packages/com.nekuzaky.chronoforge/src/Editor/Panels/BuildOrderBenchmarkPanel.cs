using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Authoring + live status for benchmarks — the timing/supply checkpoints a build order aims
    /// to hit. Each row edits one checkpoint and shows a pass/fail dot with a computed detail
    /// (e.g. "supply 34/30 · done 3:16") straight from the last evaluation.
    /// </summary>
    public sealed class BuildOrderBenchmarkPanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly VisualElement _body;

        public BuildOrderBenchmarkPanel(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("BENCHMARKS");
            header.AddToClassList("cf-section-header");
            Add(header);

            _body = new VisualElement();
            Add(_body);

            var add = new Button(() => _context.AddBenchmark()) { text = "+ Benchmark" };
            add.AddToClassList("cf-chip");
            add.style.marginLeft = 8;
            add.style.marginRight = 8;
            add.style.marginTop = 3;
            Add(add);

            Rebuild();
        }

        public void Rebuild()
        {
            _body.Clear();
            if (_context.m_Asset == null)
                return;

            List<BuildOrderBenchmark> benchmarks = _context.m_Asset.m_Benchmarks;
            if (benchmarks.Count == 0)
            {
                var empty = new Label("No benchmarks. Add checkpoints to catch timing slippage.");
                empty.AddToClassList("cf-empty__text");
                empty.style.paddingLeft = 8;
                empty.style.paddingTop = 4;
                _body.Add(empty);
                return;
            }

            for (int i = 0; i < benchmarks.Count; i++)
                _body.Add(BuildRow(benchmarks[i], i));
        }

        private VisualElement BuildRow(BuildOrderBenchmark benchmark, int index)
        {
            var card = new VisualElement();
            card.AddToClassList("cf-benchmark");

            // Header line: status dot + label + remove.
            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.alignItems = Align.Center;

            bool hasResult = _context.m_Evaluation.TryGetBenchmarkResult(index, out BuildOrderBenchmarkResult result);
            var dot = new VisualElement();
            dot.AddToClassList("cf-benchmark__dot");
            dot.EnableInClassList("cf-benchmark__dot--pass", hasResult && result.m_Passed);
            dot.EnableInClassList("cf-benchmark__dot--fail", hasResult && !result.m_Passed);
            head.Add(dot);

            var label = new TextField { value = benchmark.m_Label, isDelayed = true };
            label.style.flexGrow = 1;
            label.RegisterValueChangedCallback(evt => Commit(() => benchmark.m_Label = evt.newValue, "Edit Benchmark Label"));
            head.Add(label);

            var remove = new Button(() => _context.RemoveBenchmark(benchmark)) { text = "×" };
            remove.style.width = 22;
            head.Add(remove);
            card.Add(head);

            // Anchor time.
            var time = new TextField("At (M:SS)") { value = benchmark.DisplayAnchor, isDelayed = true };
            time.RegisterValueChangedCallback(evt =>
            {
                if (BuildOrderTime.TryParse(evt.newValue, out float seconds))
                    Commit(() => benchmark.m_AnchorTimeSeconds = seconds, "Edit Benchmark Time");
            });
            card.Add(time);

            // Supply target.
            var supplyRow = new VisualElement();
            supplyRow.style.flexDirection = FlexDirection.Row;
            supplyRow.style.alignItems = Align.Center;

            var checkSupply = new Toggle("Supply ≥") { value = benchmark.m_CheckSupply };
            checkSupply.RegisterValueChangedCallback(evt => Commit(() => benchmark.m_CheckSupply = evt.newValue, "Toggle Benchmark Supply"));
            supplyRow.Add(checkSupply);

            var expected = new IntegerField { value = benchmark.m_ExpectedSupply, isDelayed = true };
            expected.style.width = 56;
            expected.SetEnabled(benchmark.m_CheckSupply);
            expected.RegisterValueChangedCallback(evt => Commit(() => benchmark.m_ExpectedSupply = evt.newValue, "Edit Benchmark Supply"));
            supplyRow.Add(expected);
            card.Add(supplyRow);

            // Required step.
            card.Add(BuildRequiredStepField(benchmark));

            // Detail from last evaluation.
            string detail = DetailFor(index);
            if (!string.IsNullOrEmpty(detail))
            {
                var detailLabel = new Label(detail);
                detailLabel.AddToClassList("cf-benchmark__detail");
                card.Add(detailLabel);
            }

            return card;
        }

        private VisualElement BuildRequiredStepField(BuildOrderBenchmark benchmark)
        {
            var ids = new List<string> { "" };
            var choices = new List<string> { "(no required step)" };
            foreach (BuildOrderStep step in _context.m_Asset.m_Steps)
            {
                ids.Add(step.m_Id);
                choices.Add(string.IsNullOrWhiteSpace(step.m_Title) ? $"({step.m_Type})" : step.m_Title);
            }

            int current = ids.IndexOf(benchmark.m_RequiredStepId);
            if (current < 0)
                current = 0;

            var dropdown = new DropdownField("Requires", choices, current);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int selected = choices.IndexOf(evt.newValue);
                string id = selected >= 0 ? ids[selected] : "";
                Commit(() => benchmark.m_RequiredStepId = id, "Edit Benchmark Step");
            });
            return dropdown;
        }

        #region Status
        private string DetailFor(int index) =>
            _context.m_Evaluation.TryGetBenchmarkResult(index, out BuildOrderBenchmarkResult result) ? result.m_Detail : "";
        #endregion

        private void Commit(Action mutation, string undoName)
        {
            _context.RecordUndo(undoName);
            mutation();
            _context.NotifyChanged();
        }
    }
}
