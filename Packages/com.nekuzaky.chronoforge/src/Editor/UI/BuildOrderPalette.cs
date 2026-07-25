using UnityEngine;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Editor-side view of the shared palette. Colours live in <see cref="BuildOrderColors"/>
    /// (runtime) so the window and the in-game overlay stay identical; this only adds the USS
    /// class mapping the editor needs.
    /// </summary>
    public static class BuildOrderPalette
    {
        public static Color TypeColor(BuildOrderActionType type) => BuildOrderColors.TypeColor(type);

        public static string SeverityClass(BuildOrderIssueSeverity severity) => severity switch
        {
            BuildOrderIssueSeverity.Error => "cf-issue--error",
            BuildOrderIssueSeverity.Warning => "cf-issue--warning",
            _ => "cf-issue--info"
        };
    }
}
