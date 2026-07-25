using System;
using System.Text;
using UnityEngine;

namespace Chronoforge
{
    /// <summary>
    /// Versioned import/export for build orders. JSON round-trips the full asset through a
    /// stable schema; text export is a human-readable share format. No editor dependency, so
    /// a companion or overlay can reuse it verbatim.
    /// </summary>
    public static class BuildOrderSerializer
    {
        [Serializable]
        private struct SchemaHeader
        {
            public int m_SchemaVersion;
        }

        #region JSON
        public static string ExportJson(BuildOrderAsset asset)
        {
            if (asset == null)
                return "{}";
            asset.m_SchemaVersion = BuildOrderAsset.k_SchemaVersion;
            return JsonUtility.ToJson(asset, prettyPrint: true);
        }

        /// <summary>
        /// Builds a fresh detached asset from JSON — used to rehydrate snapshots for comparison.
        /// Returns null on failure. The caller owns the instance (destroy it when done).
        /// </summary>
        public static BuildOrderAsset CreateFromJson(string json, out string error)
        {
            var asset = ScriptableObject.CreateInstance<BuildOrderAsset>();
            if (ImportJson(json, asset, out error))
                return asset;

            Object.DestroyImmediate(asset);
            return null;
        }

        /// <summary>
        /// Overwrites <paramref name="target"/> from JSON. Returns false and leaves the target
        /// untouched when the payload is unreadable or from an unsupported future schema.
        /// </summary>
        public static bool ImportJson(string json, BuildOrderAsset target, out string error)
        {
            error = "";
            if (target == null)
            {
                error = "No target asset.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Empty JSON payload.";
                return false;
            }

            int version;
            try
            {
                version = JsonUtility.FromJson<SchemaHeader>(json).m_SchemaVersion;
            }
            catch (Exception exception)
            {
                error = $"Malformed JSON: {exception.Message}";
                return false;
            }

            if (version > BuildOrderAsset.k_SchemaVersion)
            {
                error = $"Schema v{version} is newer than this Chronoforge build (v{BuildOrderAsset.k_SchemaVersion}).";
                return false;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(json, target);
            }
            catch (Exception exception)
            {
                error = $"Import failed: {exception.Message}";
                return false;
            }

            target.m_SchemaVersion = BuildOrderAsset.k_SchemaVersion;
            return true;
        }
        #endregion

        #region Text
        public static string ExportText(BuildOrderAsset asset)
        {
            if (asset == null)
                return "";

            var builder = new StringBuilder();
            builder.Append(asset.m_Title);
            if (!string.IsNullOrEmpty(asset.m_Game) || !string.IsNullOrEmpty(asset.m_Faction))
                builder.Append($" ({asset.m_Game} · {asset.m_Faction})");
            builder.AppendLine();

            if (!string.IsNullOrEmpty(asset.m_Description))
                builder.AppendLine(asset.m_Description);
            builder.AppendLine();

            foreach (BuildOrderStep step in asset.m_Steps)
            {
                string optional = step.m_Optional ? "~" : " ";
                builder.AppendLine(
                    $"{optional}[{step.m_Supply,3}] {step.DisplayTime,5}  {step.m_Type,-8} {step.m_Title}");
            }

            return builder.ToString();
        }
        #endregion
    }
}
