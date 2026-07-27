using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Import/export and history actions. Text and JSON export go through the runtime serializer;
    /// import round-trips JSON back into the asset with schema checks; snapshots capture the
    /// current state for later comparison or restore.
    /// </summary>
    public sealed class BuildOrderExportPanel : VisualElement
    {
        private readonly BuildOrderEditorContext _context;

        public BuildOrderExportPanel(BuildOrderEditorContext context)
        {
            _context = context;

            var header = new Label("IMPORT / EXPORT");
            header.AddToClassList("cf-section-header");
            Add(header);

            Add(MakeButton("Export Text…", ExportText));
            Add(MakeButton("Export JSON…", ExportJson));
            Add(MakeButton("Import JSON…", ImportJson));
            Add(MakeButton("Import CSV…", ImportCsv));

            var history = new Label("HISTORY");
            history.AddToClassList("cf-section-header");
            Add(history);

            Add(MakeButton("Take Snapshot", TakeSnapshot));
        }

        #region Actions
        private void ExportText()
        {
            BuildOrderAsset asset = _context.m_Asset;
            string path = EditorUtility.SaveFilePanel("Export Build Order (Text)", "", asset.m_Title + ".txt", "txt");
            if (string.IsNullOrEmpty(path))
                return;
            File.WriteAllText(path, BuildOrderSerializer.ExportText(asset));
            AssetDatabase.Refresh();
        }

        private void ExportJson()
        {
            BuildOrderAsset asset = _context.m_Asset;
            string path = EditorUtility.SaveFilePanel("Export Build Order (JSON)", "", asset.m_Title + ".json", "json");
            if (string.IsNullOrEmpty(path))
                return;
            File.WriteAllText(path, BuildOrderSerializer.ExportJson(asset));
            AssetDatabase.Refresh();
        }

        private void ImportJson()
        {
            string path = EditorUtility.OpenFilePanel("Import Build Order (JSON)", "", "json");
            if (string.IsNullOrEmpty(path))
                return;

            string json = File.ReadAllText(path);
            _context.RecordUndo("Import Build Order");
            if (BuildOrderSerializer.ImportJson(json, _context.m_Asset, out string error))
            {
                _context.Load(_context.m_Asset);
            }
            else
            {
                EditorUtility.DisplayDialog("Chronoforge — Import failed", error, "OK");
            }
        }

        private void ImportCsv()
        {
            string path = EditorUtility.OpenFilePanel("Import Build Order (CSV/TSV)", "", "csv,tsv,txt");
            if (string.IsNullOrEmpty(path))
                return;

            string text = File.ReadAllText(path);
            List<BuildOrderStep> parsed = BuildOrderCsvImporter.Parse(text, out string error);
            if (parsed.Count == 0)
            {
                EditorUtility.DisplayDialog("Chronoforge — CSV import", string.IsNullOrEmpty(error) ? "No rows found." : error, "OK");
                return;
            }

            bool replace = EditorUtility.DisplayDialog(
                "Chronoforge — CSV import",
                $"Parsed {parsed.Count} steps. Replace the current build order, or append?",
                "Replace",
                "Append");

            _context.RecordUndo(replace ? "Import CSV (replace)" : "Import CSV (append)");
            if (replace)
                _context.m_Asset.m_Steps.Clear();
            _context.m_Asset.m_Steps.AddRange(parsed);
            _context.Load(_context.m_Asset);
        }

        private void TakeSnapshot()
        {
            BuildOrderAsset asset = _context.m_Asset;
            _context.RecordUndo("Take Snapshot");
            asset.m_Snapshots.Add(new BuildOrderSnapshot
            {
                m_Id = Guid.NewGuid().ToString("N"),
                m_Label = $"Snapshot {asset.m_Snapshots.Count + 1}",
                m_TimestampUtc = DateTime.UtcNow.ToString("O"),
                m_Author = asset.m_Author,
                m_Json = BuildOrderSerializer.ExportJson(asset, includeHistory: false)
            });
            _context.NotifyChanged();
        }
        #endregion

        private Button MakeButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("cf-chip");
            button.style.marginLeft = 8;
            button.style.marginRight = 8;
            button.style.marginBottom = 3;
            return button;
        }
    }
}
