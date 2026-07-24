using UnityEngine;

namespace Chronoforge.Editor
{
    /// <summary>Central colour map so type badges and timeline bars stay consistent everywhere.</summary>
    public static class BuildOrderPalette
    {
        public static Color TypeColor(BuildOrderActionType type) => type switch
        {
            BuildOrderActionType.Unit => new Color(0.30f, 0.60f, 1.00f),
            BuildOrderActionType.Building => new Color(0.55f, 0.45f, 0.90f),
            BuildOrderActionType.Upgrade => new Color(0.35f, 0.75f, 0.65f),
            BuildOrderActionType.Economy => new Color(0.95f, 0.78f, 0.30f),
            BuildOrderActionType.Scout => new Color(0.50f, 0.80f, 0.40f),
            BuildOrderActionType.Attack => new Color(0.90f, 0.35f, 0.35f),
            BuildOrderActionType.Expand => new Color(0.40f, 0.85f, 0.80f),
            BuildOrderActionType.Tech => new Color(0.65f, 0.55f, 0.95f),
            BuildOrderActionType.Defense => new Color(0.80f, 0.55f, 0.35f),
            BuildOrderActionType.Note => new Color(0.55f, 0.58f, 0.62f),
            _ => new Color(0.70f, 0.72f, 0.75f)
        };

        public static string SeverityClass(BuildOrderIssueSeverity severity) => severity switch
        {
            BuildOrderIssueSeverity.Error => "cf-issue--error",
            BuildOrderIssueSeverity.Warning => "cf-issue--warning",
            _ => "cf-issue--info"
        };
    }
}
