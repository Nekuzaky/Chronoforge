using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Live list of validation issues from the last evaluation. Each issue is colour-coded by
    /// severity and jumps to its step on click. Shows an "all clear" state when empty.
    /// </summary>
    public sealed class BuildOrderValidationPanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;
        private readonly ScrollView _body;

        public BuildOrderValidationPanel(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("VALIDATION");
            header.AddToClassList("cf-section-header");
            Add(header);

            _body = new ScrollView(ScrollViewMode.Vertical);
            _body.style.flexGrow = 1;
            Add(_body);

            Rebuild();
        }

        public void Rebuild()
        {
            _body.Clear();

            var issues = _context.m_Evaluation.m_Issues;
            if (issues.Count == 0)
            {
                var ok = new Label("No issues. Build order is clean.");
                ok.AddToClassList("cf-empty__text");
                ok.style.paddingLeft = 8;
                ok.style.paddingTop = 6;
                _body.Add(ok);
                return;
            }

            foreach (BuildOrderValidationIssue issue in issues)
                _body.Add(BuildIssueRow(issue));
        }

        private VisualElement BuildIssueRow(BuildOrderValidationIssue issue)
        {
            var row = new VisualElement();
            row.AddToClassList("cf-issue");
            row.AddToClassList(BuildOrderPalette.SeverityClass(issue.m_Severity));

            var dot = new VisualElement();
            dot.AddToClassList("cf-issue__dot");
            row.Add(dot);

            var message = new Label(issue.m_Message);
            message.AddToClassList("cf-issue__msg");
            row.Add(message);

            if (!string.IsNullOrEmpty(issue.m_StepId))
                row.RegisterCallback<ClickEvent>(_ => _context.Select(issue.m_StepId));

            return row;
        }
    }
}
