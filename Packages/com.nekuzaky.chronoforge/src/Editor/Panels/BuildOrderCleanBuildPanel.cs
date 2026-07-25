using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Settings for the clean-build analysis. Collapsed to a single toggle until enabled, so the
    /// panel stays out of the way for users who only want sequence authoring. Once on, it shows a
    /// live count of execution-quality issues and the thresholds that produced them.
    /// </summary>
    public sealed class BuildOrderCleanBuildPanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly VisualElement _body;

        public BuildOrderCleanBuildPanel(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("CLEAN BUILD");
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

            BuildOrderCleanBuildSettings settings = _context.m_Asset.m_CleanBuild;

            var enabled = new Toggle("Analyse execution") { value = settings.m_Enabled };
            enabled.tooltip = "Check for supply blocks, idle production, stockpiling and worker gaps.";
            enabled.RegisterValueChangedCallback(evt => Commit(() => settings.m_Enabled = evt.newValue, "Toggle Clean Build"));
            _body.Add(enabled);

            if (!settings.m_Enabled)
            {
                var hint = new Label("Off. Enable to check execution quality, not just sequence validity.");
                hint.AddToClassList("cf-empty__text");
                hint.style.paddingLeft = 8;
                _body.Add(hint);
                return;
            }

            _body.Add(Summary());
            _body.Add(Check("Supply blocks", settings.m_CheckSupplyBlocks, v => settings.m_CheckSupplyBlocks = v));
            _body.Add(Number("Starting supply cap", settings.m_StartingSupplyCap, v => settings.m_StartingSupplyCap = v));
            _body.Add(Check("Idle production", settings.m_CheckIdleProduction, v => settings.m_CheckIdleProduction = v));
            _body.Add(Number("Max idle (s)", settings.m_MaxFacilityIdleSeconds, v => settings.m_MaxFacilityIdleSeconds = v));
            _body.Add(Check("Worker gaps", settings.m_CheckWorkerGaps, v => settings.m_CheckWorkerGaps = v));
            _body.Add(Number("Max worker gap (s)", settings.m_MaxWorkerGapSeconds, v => settings.m_MaxWorkerGapSeconds = v));
            _body.Add(Check("Stockpiling", settings.m_CheckStockpiling, v => settings.m_CheckStockpiling = v));
            _body.Add(Number("Stockpile threshold", settings.m_StockpileThreshold, v => settings.m_StockpileThreshold = v));
        }

        private Label Summary()
        {
            int count = CountExecutionIssues();
            var label = new Label(count == 0
                ? "Execution looks clean."
                : $"{count} execution issue{(count == 1 ? "" : "s")} — see validation.");
            label.AddToClassList("cf-benchmark__detail");
            label.style.paddingLeft = 8;
            return label;
        }

        private int CountExecutionIssues()
        {
            int count = 0;
            foreach (BuildOrderValidationIssue issue in _context.m_Evaluation.m_Issues)
            {
                if (IsExecutionCode(issue.m_Code))
                    count++;
            }
            return count;
        }

        private static bool IsExecutionCode(BuildOrderValidationCode code) =>
            code is BuildOrderValidationCode.SupplyBlock
                 or BuildOrderValidationCode.IdleProduction
                 or BuildOrderValidationCode.WorkerProductionGap
                 or BuildOrderValidationCode.ResourceStockpiling;

        #region Field helpers
        private Toggle Check(string label, bool value, Action<bool> setter)
        {
            var toggle = new Toggle(label) { value = value };
            toggle.RegisterValueChangedCallback(evt => Commit(() => setter(evt.newValue), $"Toggle {label}"));
            return toggle;
        }

        private IntegerField Number(string label, int value, Action<int> setter)
        {
            var field = new IntegerField(label) { value = value, isDelayed = true };
            field.RegisterValueChangedCallback(evt => Commit(() => setter(evt.newValue), $"Edit {label}"));
            return field;
        }

        private FloatField Number(string label, float value, Action<float> setter)
        {
            var field = new FloatField(label) { value = value, isDelayed = true };
            field.RegisterValueChangedCallback(evt => Commit(() => setter(evt.newValue), $"Edit {label}"));
            return field;
        }

        private void Commit(Action mutation, string undoName)
        {
            _context.RecordUndo(undoName);
            mutation();
            _context.NotifyChanged();
        }
        #endregion
    }
}
