using System.Collections.Generic;
using NUnit.Framework;
using static Chronoforge.Tests.BuildOrderTestFactory;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderStepClipboardTests
    {
        [Test]
        public void RoundTrip_PreservesContentWithFreshIds()
        {
            var source = new List<BuildOrderStep>
            {
                Step("a", "Pool", supply: 13, time: 24, type: BuildOrderActionType.Building),
                Step("b", "Overlord", supply: 13, time: 30, delta: 8)
            };

            string payload = BuildOrderStepClipboard.ExportSteps(source);
            Assert.IsTrue(BuildOrderStepClipboard.TryImportSteps(payload, out List<BuildOrderStep> pasted, out string error), error);

            Assert.AreEqual(2, pasted.Count);
            Assert.AreEqual("Pool", pasted[0].m_Title);
            Assert.AreEqual(BuildOrderActionType.Building, pasted[0].m_Type);
            Assert.AreEqual(8, pasted[1].m_PopulationDelta);

            // Fresh ids are what makes pasting into the source asset safe.
            Assert.AreNotEqual("a", pasted[0].m_Id);
            Assert.AreNotEqual("b", pasted[1].m_Id);
            Assert.AreNotEqual(pasted[0].m_Id, pasted[1].m_Id);
        }

        [Test]
        public void Export_EmptySelection_ReturnsEmptyString()
        {
            Assert.IsEmpty(BuildOrderStepClipboard.ExportSteps(new List<BuildOrderStep>()));
            Assert.IsEmpty(BuildOrderStepClipboard.ExportSteps(null));
        }

        [Test]
        public void Import_ForeignClipboardText_IsRejected()
        {
            Assert.IsFalse(BuildOrderStepClipboard.TryImportSteps("just some copied text", out _, out string error));
            Assert.IsNotEmpty(error);

            Assert.IsFalse(BuildOrderStepClipboard.TryImportSteps("{\"m_Marker\":\"something.else\"}", out _, out _));
            Assert.IsFalse(BuildOrderStepClipboard.TryImportSteps("", out _, out _));
        }

        [Test]
        public void Import_DeepCopiesNestedData()
        {
            BuildOrderStep step = Step("a", "Marine");
            step.m_ResourceCost.m_Amounts.Add(new BuildOrderResourceAmount("minerals", 50f));
            step.m_TagIds.Add("tag-1");

            string payload = BuildOrderStepClipboard.ExportSteps(new List<BuildOrderStep> { step });
            Assert.IsTrue(BuildOrderStepClipboard.TryImportSteps(payload, out List<BuildOrderStep> pasted, out _));

            pasted[0].m_ResourceCost.m_Amounts[0].m_Amount = 999f;
            pasted[0].m_TagIds.Add("tag-2");

            Assert.AreEqual(50f, step.m_ResourceCost.GetAmount("minerals"), "source cost must not be aliased");
            Assert.AreEqual(1, step.m_TagIds.Count, "source tags must not be aliased");
        }
    }
}
