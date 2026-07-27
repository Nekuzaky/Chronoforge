using NUnit.Framework;
using static Chronoforge.Tests.BuildOrderTestFactory;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderStepCloneTests
    {
        [Test]
        public void Clone_GivesFreshId()
        {
            BuildOrderStep original = Step("a", "Marine");

            BuildOrderStep clone = original.Clone();

            Assert.AreNotEqual(original.m_Id, clone.m_Id);
            Assert.AreEqual("Marine", clone.m_Title);
        }

        [Test]
        public void Clone_DoesNotAliasResourceCost()
        {
            BuildOrderStep original = Step("a", "Marine");
            original.m_ResourceCost.m_Amounts.Add(new BuildOrderResourceAmount("minerals", 50f));

            BuildOrderStep clone = original.Clone();
            clone.m_ResourceCost.m_Amounts[0].m_Amount = 999f;

            Assert.AreEqual(50f, original.m_ResourceCost.GetAmount("minerals"));
        }

        [Test]
        public void Clone_DoesNotAliasPrerequisites()
        {
            BuildOrderStep original = Step("a", "Marine");
            original.m_Prerequisites.Add(new BuildOrderRequirement
            {
                m_Type = BuildOrderRequirementType.Building,
                m_TargetId = "barracks"
            });

            BuildOrderStep clone = original.Clone();
            clone.m_Prerequisites[0].m_TargetId = "factory";

            Assert.AreEqual("barracks", original.m_Prerequisites[0].m_TargetId);
        }

        [Test]
        public void Clone_DoesNotAliasTags()
        {
            BuildOrderStep original = Step("a", "Marine");
            original.m_TagIds.Add("opener");

            BuildOrderStep clone = original.Clone();
            clone.m_TagIds.Add("allin");

            Assert.AreEqual(1, original.m_TagIds.Count);
        }
    }
}
