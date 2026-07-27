using NUnit.Framework;
using UnityEngine;
using static Chronoforge.Tests.BuildOrderTestFactory;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderSerializerTests
    {
        [Test]
        public void JsonRoundTrip_PreservesSteps()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Title = "Round Trip";
            asset.m_Steps.Add(Step("a", "Worker", supply: 12, time: 0));
            asset.m_Steps.Add(Step("b", "Pool", supply: 13, time: 24));

            string json = BuildOrderSerializer.ExportJson(asset);
            BuildOrderAsset restored = BuildOrderSerializer.CreateFromJson(json, out string error);

            Assert.IsNotNull(restored, error);
            Assert.AreEqual(2, restored.m_Steps.Count);
            Assert.AreEqual("Round Trip", restored.m_Title);
            Object.DestroyImmediate(restored);
        }

        [Test]
        public void SnapshotPayload_ExcludesHistory()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("a", "Worker"));
            asset.m_Snapshots.Add(new BuildOrderSnapshot { m_Id = "s1", m_Label = "old", m_Json = "{}" });

            string json = BuildOrderSerializer.ExportJson(asset, includeHistory: false);
            BuildOrderAsset restored = BuildOrderSerializer.CreateFromJson(json, out _);

            Assert.IsNotNull(restored);
            Assert.AreEqual(0, restored.m_Snapshots.Count);
            Assert.AreEqual(1, asset.m_Snapshots.Count, "source history must be untouched");
            Object.DestroyImmediate(restored);
        }

        [Test]
        public void ImportJson_RejectsNewerSchema()
        {
            BuildOrderAsset target = Asset();
            bool ok = BuildOrderSerializer.ImportJson("{\"m_SchemaVersion\":9999}", target, out string error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }
    }
}
