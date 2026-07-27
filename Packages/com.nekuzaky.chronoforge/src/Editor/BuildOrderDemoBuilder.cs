using Chronoforge.Demo;
using Chronoforge.Overlay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Generates the demo: a rich sample build order plus a scene wired to play it back. Built
    /// programmatically so the scene and asset are always valid for the running Unity version —
    /// no hand-authored YAML to drift.
    /// <para>
    /// The sample deliberately contains two flaws — a worker-production gap and a benchmark it
    /// misses — because a flawless build order would demonstrate none of the analysis the tool
    /// exists for. Both are called out in the asset description.
    /// </para>
    /// </summary>
    public static class BuildOrderDemoBuilder
    {
        private const string k_Folder = "Assets/Chronoforge Demo";
        private const string k_AssetPath = k_Folder + "/SO_DemoBuildOrder.asset";
        private const string k_ScenePath = k_Folder + "/Chronoforge Demo.unity";

        private const string k_Minerals = "minerals";
        private const string k_Gas = "gas";
        private const string k_Barracks = "barracks";
        private const string k_Factory = "factory";

        [MenuItem("Chronoforge/Create Demo Scene", priority = 20)]
        public static void CreateDemoScene()
        {
            // Never discard the user's open scene silently.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            BuildOrderAsset asset = CreateSampleAsset();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var host = new GameObject("Chronoforge Demo Player");
            var document = host.AddComponent<UIDocument>();
            var overlay = host.AddComponent<BuildOrderOverlay>();
            var player = host.AddComponent<BuildOrderDemoPlayer>();

            overlay.m_BuildOrder = asset;
            overlay.m_UseInternalClock = false;
            overlay.m_UpcomingCount = 4;
            player.m_AutoPlay = true;

            bool configured = BuildOrderOverlaySetup.Configure(document, out string setupError);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, k_ScenePath);

            string message =
                "Demo scene created at:\n" + k_ScenePath +
                "\n\nPress Play: the overlay tracks the build order in real time; the transport at " +
                "the bottom lets you pause, scrub and change speed." +
                "\n\nThen select SO_DemoBuildOrder and press Open in Chronoforge — the sample " +
                "deliberately contains a worker gap and a missed benchmark so the validation and " +
                "clean-build panels have something to show.";
            if (!configured)
                message += "\n\nThe overlay still needs PanelSettings:\n" + setupError;

            EditorUtility.DisplayDialog("Chronoforge", message, "OK");
            Selection.activeObject = asset;
        }

        [MenuItem("Chronoforge/Regenerate Demo Build Order", priority = 22)]
        private static void RegenerateAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BuildOrderAsset>(k_AssetPath);
            if (asset == null)
            {
                CreateSampleAsset();
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Chronoforge — Regenerate demo",
                "Overwrite " + k_AssetPath + " with a fresh sample? Any edits to it are lost.",
                "Regenerate",
                "Cancel");
            if (!confirmed)
                return;

            Undo.RecordObject(asset, "Regenerate Demo Build Order");
            Clear(asset);
            Populate(asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }

        #region Asset
        private static BuildOrderAsset CreateSampleAsset()
        {
            EnsureFolder();

            var asset = AssetDatabase.LoadAssetAtPath<BuildOrderAsset>(k_AssetPath);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<BuildOrderAsset>();
            Populate(asset);
            AssetDatabase.CreateAsset(asset, k_AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void Clear(BuildOrderAsset asset)
        {
            asset.m_Steps.Clear();
            asset.m_Branches.Clear();
            asset.m_Tags.Clear();
            asset.m_Benchmarks.Clear();
            asset.m_ResourceModel.Clear();
        }

        private static void Populate(BuildOrderAsset asset)
        {
            BuildMetadata(asset);
            BuildEconomy(asset);
            BuildTags(asset);
            BuildBranch(asset);
            BuildSteps(asset);
            BuildBenchmarks(asset);
            BuildCleanBuildSettings(asset);
        }

        private static void BuildMetadata(BuildOrderAsset asset)
        {
            asset.m_Title = "Two-Base Mech Opening (Demo)";
            asset.m_Game = "Generic RTS";
            asset.m_Faction = "Vanguard";
            asset.m_Author = "Chronoforge sample";
            asset.m_Description =
                "A worked example covering every Chronoforge feature: typed steps, resource costs, " +
                "prerequisites, tags, a conditional branch, benchmarks and the clean-build analysis.\n\n" +
                "It contains two DELIBERATE flaws so the analysis has something to report:\n" +
                "  • worker production pauses for 45 s around 1:15\n" +
                "  • the \"Factory online\" benchmark is missed\n" +
                "Open the Validation and Clean Build panels to see both.";
        }

        private static void BuildEconomy(BuildOrderAsset asset)
        {
            asset.m_ResourceModel.Add(new BuildOrderResourceRate
            {
                m_ResourceId = k_Minerals,
                m_StartingAmount = 50f,
                m_IncomePerSecond = 7f
            });
            asset.m_ResourceModel.Add(new BuildOrderResourceRate
            {
                m_ResourceId = k_Gas,
                m_StartingAmount = 0f,
                m_IncomePerSecond = 2f
            });
        }

        private static void BuildTags(BuildOrderAsset asset)
        {
            asset.m_Tags.Add(new BuildOrderTag { m_Id = "economy", m_Label = "economy", m_ColorHex = "#F2C74C" });
            asset.m_Tags.Add(new BuildOrderTag { m_Id = "army", m_Label = "army", m_ColorHex = "#E0594A" });
            asset.m_Tags.Add(new BuildOrderTag { m_Id = "tech", m_Label = "tech", m_ColorHex = "#A68CF2" });
        }

        private static void BuildBranch(BuildOrderAsset asset)
        {
            asset.m_Branches.Add(new BuildOrderBranch
            {
                m_Key = "vs-rush",
                m_Name = "Against an early rush",
                m_Description = "Taken when scouting finds early aggression: bunker first, expansion delayed.",
                m_ColorHex = "#E0894A",
                m_Condition = new BuildOrderCondition
                {
                    m_Variable = "enemy.earlyAggression",
                    m_Operator = BuildOrderConditionOperator.Equals,
                    m_Value = "true",
                    m_Description = "Scout saw a rush build."
                }
            });
        }

        private static void BuildSteps(BuildOrderAsset asset)
        {
            // Opening economy.
            Worker(asset, supply: 12, time: 0f);
            Worker(asset, supply: 13, time: 12f);
            Depot(asset, supply: 14, time: 26f);
            Worker(asset, supply: 14, time: 30f);

            // First production. The barracks takes 46 s to finish.
            BuildOrderStep barracks = Add(asset, BuildOrderActionType.Building, "Barracks", supply: 15, time: 50f,
                minerals: 150f, duration: 46f, tag: "army");
            barracks.m_ProvidesFacilityId = k_Barracks;

            Add(asset, BuildOrderActionType.Economy, "Refinery", supply: 16, time: 64f, minerals: 75f, tag: "economy");

            // DELIBERATE FLAW: no worker between 0:30 and 1:23 — a 53 s gap the analysis reports.
            Worker(asset, supply: 17, time: 83f);

            Marine(asset, supply: 18, time: 98f);
            Marine(asset, supply: 19, time: 118f);

            BuildOrderStep scout = Add(asset, BuildOrderActionType.Scout, "Scout with a marine", supply: 19, time: 130f);
            scout.m_DesignerNotes = "Decides whether to take the vs-rush branch.";
            scout.m_Prerequisites.Add(new BuildOrderRequirement
            {
                m_Type = BuildOrderRequirementType.Step,
                m_TargetId = asset.m_Steps[asset.m_Steps.Count - 2].m_Id,
                m_Label = "a marine exists"
            });

            // The defensive variation, gated by the branch condition. Kept in time order with the
            // rest so it doesn't read as a sequencing mistake.
            BuildOrderStep bunker = Add(asset, BuildOrderActionType.Defense, "Bunker at the ramp",
                supply: 19, time: 135f, minerals: 100f, tag: "army");
            bunker.m_BranchKey = "vs-rush";
            bunker.m_Optional = true;
            bunker.m_DesignerNotes = "Only if the scout found early aggression.";

            Depot(asset, supply: 20, time: 145f);
            Add(asset, BuildOrderActionType.Expand, "Second base", supply: 20, time: 160f, minerals: 400f, tag: "economy");

            // Tech into mech.
            BuildOrderStep factory = Add(asset, BuildOrderActionType.Building, "Factory", supply: 21, time: 190f,
                minerals: 200f, gas: 100f, duration: 60f, tag: "tech");
            factory.m_ProvidesFacilityId = k_Factory;
            factory.m_Prerequisites.Add(new BuildOrderRequirement
            {
                m_Type = BuildOrderRequirementType.Building,
                m_TargetId = k_Barracks,
                m_Label = "Barracks"
            });

            Add(asset, BuildOrderActionType.Upgrade, "Weapons +1", supply: 21, time: 215f,
                minerals: 100f, gas: 100f, duration: 114f, tag: "tech");

            Depot(asset, supply: 22, time: 240f);

            Tank(asset, supply: 25, time: 258f);
            Tank(asset, supply: 28, time: 292f);

            BuildOrderStep note = Add(asset, BuildOrderActionType.Note, "Hold position until tanks are out",
                supply: 28, time: 300f);
            note.m_DesignerNotes = "Reminder rather than an action — Note steps cost nothing and produce nothing.";
        }

        private static void BuildBenchmarks(BuildOrderAsset asset)
        {
            // Passes: supply is comfortably past 14 by 1:00.
            asset.m_Benchmarks.Add(new BuildOrderBenchmark
            {
                m_Label = "Economy on track",
                m_AnchorTimeSeconds = 60f,
                m_CheckSupply = true,
                m_ExpectedSupply = 3
            });

            // DELIBERATE FLAW: the factory finishes at 4:10, well past this 3:20 target.
            BuildOrderStep factory = asset.m_Steps.Find(step => step.m_ProvidesFacilityId == k_Factory);
            asset.m_Benchmarks.Add(new BuildOrderBenchmark
            {
                m_Label = "Factory online",
                m_AnchorTimeSeconds = 200f,
                m_RequiredStepId = factory != null ? factory.m_Id : "",
                m_ToleranceSeconds = 5f
            });

            asset.m_Benchmarks.Add(new BuildOrderBenchmark
            {
                m_Label = "Mech push ready",
                m_AnchorTimeSeconds = 300f,
                m_CheckSupply = true,
                m_ExpectedSupply = 12
            });
        }

        private static void BuildCleanBuildSettings(BuildOrderAsset asset)
        {
            asset.m_CleanBuild.m_Enabled = true;
            asset.m_CleanBuild.m_StartingSupplyCap = 15;
            asset.m_CleanBuild.m_MaxWorkerGapSeconds = 20f;
            asset.m_CleanBuild.m_MaxFacilityIdleSeconds = 30f;
            asset.m_CleanBuild.m_StockpileThreshold = 500f;
        }
        #endregion

        #region Step helpers
        private static BuildOrderStep Add(
            BuildOrderAsset asset,
            BuildOrderActionType type,
            string title,
            int supply,
            float time,
            float minerals = 0f,
            float gas = 0f,
            int delta = 0,
            float duration = 0f,
            int supplyProvided = 0,
            string tag = "")
        {
            BuildOrderStep step = BuildOrderStep.Create(type);
            step.m_Title = title;
            step.m_Supply = supply;
            step.m_TimeSeconds = time;
            step.m_PopulationDelta = delta;
            step.m_EstimatedDuration = duration;
            step.m_SupplyProvided = supplyProvided;

            if (minerals > 0f)
                step.m_ResourceCost.m_Amounts.Add(new BuildOrderResourceAmount(k_Minerals, minerals));
            if (gas > 0f)
                step.m_ResourceCost.m_Amounts.Add(new BuildOrderResourceAmount(k_Gas, gas));
            if (!string.IsNullOrEmpty(tag))
                step.m_TagIds.Add(tag);

            asset.m_Steps.Add(step);
            return step;
        }

        private static void Worker(BuildOrderAsset asset, int supply, float time)
        {
            BuildOrderStep step = Add(asset, BuildOrderActionType.Economy, "Worker", supply, time,
                minerals: 50f, delta: 1, duration: 12f, tag: "economy");
            step.m_IsWorker = true;
        }

        private static void Depot(BuildOrderAsset asset, int supply, float time) =>
            Add(asset, BuildOrderActionType.Building, "Supply Depot", supply, time,
                minerals: 100f, duration: 21f, supplyProvided: 8, tag: "economy");

        private static void Marine(BuildOrderAsset asset, int supply, float time)
        {
            BuildOrderStep step = Add(asset, BuildOrderActionType.Unit, "Marine", supply, time,
                minerals: 50f, delta: 1, duration: 18f, tag: "army");
            step.m_ProducedByFacilityId = k_Barracks;
        }

        private static void Tank(BuildOrderAsset asset, int supply, float time)
        {
            BuildOrderStep step = Add(asset, BuildOrderActionType.Unit, "Siege Tank", supply, time,
                minerals: 150f, gas: 125f, delta: 3, duration: 32f, tag: "army");
            step.m_ProducedByFacilityId = k_Factory;
        }
        #endregion

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets", "Chronoforge Demo");
        }
    }
}
