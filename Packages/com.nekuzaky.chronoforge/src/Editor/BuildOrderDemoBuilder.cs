using Chronoforge.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Generates a ready-to-play demo scene and a sample build order, wired to a
    /// <see cref="BuildOrderDemoPlayer"/>. Built programmatically so the scene and asset are
    /// always valid — no hand-authored YAML to drift or break.
    /// </summary>
    public static class BuildOrderDemoBuilder
    {
        private const string k_Folder = "Assets/Chronoforge Demo";
        private const string k_AssetPath = k_Folder + "/SO_DemoBuildOrder.asset";
        private const string k_ScenePath = k_Folder + "/Chronoforge Demo.unity";

        [MenuItem("Chronoforge/Create Demo Scene", priority = 20)]
        public static void CreateDemoScene()
        {
            // Never discard the user's open scene silently.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            BuildOrderAsset asset = CreateSampleAsset();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var host = new GameObject("Chronoforge Demo Player");
            var player = host.AddComponent<BuildOrderDemoPlayer>();
            player.m_BuildOrder = asset;
            player.m_AutoPlay = true;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, k_ScenePath);

            EditorUtility.DisplayDialog(
                "Chronoforge",
                "Demo scene created at:\n" + k_ScenePath + "\n\nPress Play to watch the build order run, then open the asset to edit it in Chronoforge.",
                "OK");
            Selection.activeObject = asset;
        }

        private static BuildOrderAsset CreateSampleAsset()
        {
            EnsureFolder();

            var asset = AssetDatabase.LoadAssetAtPath<BuildOrderAsset>(k_AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BuildOrderAsset>();
                Populate(asset);
                AssetDatabase.CreateAsset(asset, k_AssetPath);
                AssetDatabase.SaveAssets();
            }
            return asset;
        }

        private static void Populate(BuildOrderAsset asset)
        {
            asset.m_Title = "Aggressive Opening (Demo)";
            asset.m_Game = "Generic RTS";
            asset.m_Faction = "Swarm";
            asset.m_Description = "Sample build order shipped with Chronoforge. Edit it in the editor window; press Play in the demo scene to watch it run.";

            asset.m_Steps.Add(Step(BuildOrderActionType.Economy, "Worker", supply: 12, time: 0, delta: 1, resource: 50));
            asset.m_Steps.Add(Step(BuildOrderActionType.Economy, "Worker", supply: 13, time: 12, delta: 1, resource: 50));
            asset.m_Steps.Add(Step(BuildOrderActionType.Building, "Spawning Pool", supply: 13, time: 24, delta: 0, resource: 200));
            asset.m_Steps.Add(Step(BuildOrderActionType.Unit, "Overlord", supply: 13, time: 30, delta: 8, resource: 100));
            asset.m_Steps.Add(Step(BuildOrderActionType.Scout, "Scout with worker", supply: 13, time: 35, delta: 0, resource: 0));
            asset.m_Steps.Add(Step(BuildOrderActionType.Attack, "First attack wave", supply: 16, time: 120, delta: 0, resource: 0));

            asset.m_ResourceModel.Add(new BuildOrderResourceRate
            {
                m_ResourceId = "minerals",
                m_StartingAmount = 50f,
                m_IncomePerSecond = 1.4f
            });

            asset.m_Benchmarks.Add(new BuildOrderBenchmark
            {
                m_Label = "Pool timing",
                m_AnchorTimeSeconds = 40f,
                m_CheckSupply = true,
                m_ExpectedSupply = 13
            });
        }

        private static BuildOrderStep Step(BuildOrderActionType type, string title, int supply, float time, int delta, float resource)
        {
            BuildOrderStep step = BuildOrderStep.Create(type);
            step.m_Title = title;
            step.m_Supply = supply;
            step.m_TimeSeconds = time;
            step.m_PopulationDelta = delta;
            if (resource > 0f)
                step.m_ResourceCost.m_Amounts.Add(new BuildOrderResourceAmount("minerals", resource));
            return step;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets", "Chronoforge Demo");
        }
    }
}
