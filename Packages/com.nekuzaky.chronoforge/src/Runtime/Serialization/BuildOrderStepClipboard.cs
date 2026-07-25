using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chronoforge
{
    /// <summary>
    /// Serializes a selection of steps to text and back. Text rather than an in-memory buffer so a
    /// copy survives crossing assets — and Unity sessions — via the system clipboard. Imported
    /// steps always come back with fresh ids, so pasting into the source asset cannot duplicate an
    /// id.
    /// </summary>
    public static class BuildOrderStepClipboard
    {
        private const string k_Marker = "chronoforge.steps";

        [Serializable]
        private sealed class Payload
        {
            public string m_Marker = k_Marker;
            public int m_SchemaVersion = BuildOrderAsset.k_SchemaVersion;
            public List<BuildOrderStep> m_Steps = new();
        }

        /// <summary>Returns an empty string when there is nothing to copy.</summary>
        public static string ExportSteps(List<BuildOrderStep> steps)
        {
            if (steps == null || steps.Count == 0)
                return "";

            var payload = new Payload();
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null)
                    payload.m_Steps.Add(steps[i]);
            }
            return JsonUtility.ToJson(payload);
        }

        /// <summary>
        /// Parses a clipboard payload into fresh step copies. Returns false — without touching
        /// <paramref name="steps"/> — for anything that is not a Chronoforge step payload, so
        /// pasting arbitrary clipboard text is a no-op rather than a corruption.
        /// </summary>
        public static bool TryImportSteps(string text, out List<BuildOrderStep> steps, out string error)
        {
            steps = null;
            error = "";

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Clipboard is empty.";
                return false;
            }

            Payload payload;
            try
            {
                payload = JsonUtility.FromJson<Payload>(text);
            }
            catch (Exception exception)
            {
                error = $"Clipboard does not contain steps: {exception.Message}";
                return false;
            }

            if (payload == null || payload.m_Marker != k_Marker)
            {
                error = "Clipboard does not contain Chronoforge steps.";
                return false;
            }
            if (payload.m_SchemaVersion > BuildOrderAsset.k_SchemaVersion)
            {
                error = $"Copied steps use schema v{payload.m_SchemaVersion}, newer than this build (v{BuildOrderAsset.k_SchemaVersion}).";
                return false;
            }
            if (payload.m_Steps == null || payload.m_Steps.Count == 0)
            {
                error = "No steps in the clipboard payload.";
                return false;
            }

            steps = new List<BuildOrderStep>(payload.m_Steps.Count);
            for (int i = 0; i < payload.m_Steps.Count; i++)
                steps.Add(payload.m_Steps[i].Clone());   // Clone assigns a fresh id.

            return true;
        }
    }
}
