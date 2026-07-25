using UnityEngine;

namespace Chronoforge
{
    /// <summary>
    /// The single source of truth for Chronoforge's visual identity, in the runtime assembly so
    /// the editor windows and the in-game overlay cannot drift apart.
    /// </summary>
    public static class BuildOrderColors
    {
        public static readonly Color k_Panel = new(0.094f, 0.102f, 0.118f, 0.94f);
        public static readonly Color k_Accent = new(0.298f, 0.604f, 1f);
        public static readonly Color k_Text = new(0.855f, 0.871f, 0.894f);
        public static readonly Color k_Muted = new(0.545f, 0.588f, 0.647f);
        public static readonly Color k_Warning = new(0.878f, 0.659f, 0.204f);
        public static readonly Color k_Error = new(0.878f, 0.329f, 0.290f);
        public static readonly Color k_Pass = new(0.361f, 0.722f, 0.361f);

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
    }
}
