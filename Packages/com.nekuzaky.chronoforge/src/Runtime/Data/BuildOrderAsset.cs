using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chronoforge
{
    /// <summary>
    /// The persisted build order document: metadata, ordered steps, branches, tags, an
    /// optional resource model, and its snapshot history. Pure data — all rules live in the
    /// runtime evaluation layer, all editing in the editor assembly.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_BuildOrder", menuName = "Chronoforge/Build Order", order = 0)]
    public sealed class BuildOrderAsset : ScriptableObject
    {
        /// <summary>Bumped whenever the serialized shape changes. Read by the serializer for migration.</summary>
        public const int k_SchemaVersion = 3;

        #region Metadata
        public int m_SchemaVersion = k_SchemaVersion;
        public string m_Title = "New Build Order";
        public string m_Game = "";
        public string m_Faction = "";
        [TextArea(2, 4)] public string m_Description = "";
        public string m_Author = "";
        #endregion

        #region Content
        public List<BuildOrderStep> m_Steps = new();
        public List<BuildOrderBranch> m_Branches = new();
        public List<BuildOrderTag> m_Tags = new();
        public List<BuildOrderResourceRate> m_ResourceModel = new();
        public List<BuildOrderBenchmark> m_Benchmarks = new();
        public BuildOrderCleanBuildSettings m_CleanBuild = new();
        #endregion

        #region History
        public List<BuildOrderSnapshot> m_Snapshots = new();
        #endregion

        public BuildOrderTag FindTag(string tagId) => m_Tags.Find(tag => tag.m_Id == tagId);

        public BuildOrderBranch FindBranch(string branchKey) => m_Branches.Find(branch => branch.m_Key == branchKey);

        public BuildOrderStep FindStep(string stepId) => m_Steps.Find(step => step.m_Id == stepId);
    }
}
