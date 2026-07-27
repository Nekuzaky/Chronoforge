using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Compact inspector for a <see cref="BuildOrderAsset"/>. Surfaces metadata and a live
    /// summary, and hands off real authoring to the dedicated editor window. Deliberately does
    /// not duplicate the window's step editing.
    /// </summary>
    [CustomEditor(typeof(BuildOrderAsset))]
    public sealed class BuildOrderInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var asset = (BuildOrderAsset)target;

            var open = new Button(() => BuildOrderEditorWindow.OpenFor(asset))
            {
                text = "Open in Chronoforge"
            };
            open.style.height = 26;
            open.style.marginBottom = 8;
            root.Add(open);

            root.Add(new PropertyField(serializedObject.FindProperty("m_Title")));
            root.Add(new PropertyField(serializedObject.FindProperty("m_Game")));
            root.Add(new PropertyField(serializedObject.FindProperty("m_Faction")));
            root.Add(new PropertyField(serializedObject.FindProperty("m_Author")));
            root.Add(new PropertyField(serializedObject.FindProperty("m_Description")));

            var summary = new Label(BuildSummary(asset));
            summary.style.marginTop = 8;
            summary.style.whiteSpace = WhiteSpace.Normal;
            summary.style.opacity = 0.7f;
            root.Add(summary);

            return root;
        }

        private static string BuildSummary(BuildOrderAsset asset)
        {
            var evaluation = BuildOrderEvaluation.Run(asset);
            return $"{asset.m_Steps.Count} steps · {asset.m_Branches.Count} branches · " +
                   $"{BuildOrderTime.Format(evaluation.m_TotalSeconds)} total · " +
                   $"{evaluation.ErrorCount} errors, {evaluation.WarningCount} warnings";
        }
    }
}
