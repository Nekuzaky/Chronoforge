using System.Globalization;
using UnityEngine;

namespace Chronoforge
{
    /// <summary>
    /// Shared time helpers so runtime, editor and any future overlay format
    /// <c>M:SS</c> identically. Pure functions, no allocation beyond the result string.
    /// </summary>
    public static class BuildOrderTime
    {
        public static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        public static bool TryParse(string text, out float seconds)
        {
            seconds = 0f;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string trimmed = text.Trim();
            int colon = trimmed.IndexOf(':');
            if (colon >= 0)
            {
                string minutePart = trimmed[..colon];
                string secondPart = trimmed[(colon + 1)..];
                if (int.TryParse(minutePart, out int minutes) &&
                    int.TryParse(secondPart, out int secs))
                {
                    seconds = minutes * 60 + secs;
                    return true;
                }
                return false;
            }

            return float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds);
        }
    }
}
