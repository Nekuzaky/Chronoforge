using NUnit.Framework;
using static Chronoforge.Tests.BuildOrderTestFactory;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderValidatorTests
    {
        [Test]
        public void CleanBuild_HasNoErrors()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("a", "Worker", supply: 12, time: 0));
            asset.m_Steps.Add(Step("b", "Barracks", supply: 13, time: 20));

            var issues = BuildOrderValidator.Validate(asset);

            foreach (BuildOrderValidationIssue issue in issues)
                Assert.AreNotEqual(BuildOrderIssueSeverity.Error, issue.m_Severity, issue.m_Message);
        }

        [Test]
        public void DuplicateId_IsFlagged()
        {
            BuildOrderAsset asset = Asset();
            asset.m_Steps.Add(Step("dup", "One"));
            asset.m_Steps.Add(Step("dup", "Two"));

            Assert.IsTrue(HasCode(BuildOrderValidator.Validate(asset), BuildOrderValidationCode.DuplicateId));
        }

        [Test]
        public void NegativeCost_IsFlagged()
        {
            BuildOrderAsset asset = Asset();
            BuildOrderStep step = Step("a", "Bad");
            step.m_ResourceCost.m_Amounts.Add(new BuildOrderResourceAmount("minerals", -50f));
            asset.m_Steps.Add(step);

            Assert.IsTrue(HasCode(BuildOrderValidator.Validate(asset), BuildOrderValidationCode.NegativeCost));
        }

        [Test]
        public void MissingPrerequisite_IsFlagged()
        {
            BuildOrderAsset asset = Asset();
            BuildOrderStep step = Step("a", "Needs ghost");
            step.m_Prerequisites.Add(new BuildOrderRequirement
            {
                m_Type = BuildOrderRequirementType.Step,
                m_TargetId = "ghost"
            });
            asset.m_Steps.Add(step);

            Assert.IsTrue(HasCode(BuildOrderValidator.Validate(asset), BuildOrderValidationCode.MissingPrerequisite));
        }

        [Test]
        public void BrokenBranch_IsFlagged()
        {
            BuildOrderAsset asset = Asset();
            BuildOrderStep step = Step("a", "Orphan");
            step.m_BranchKey = "undefined";
            asset.m_Steps.Add(step);

            Assert.IsTrue(HasCode(BuildOrderValidator.Validate(asset), BuildOrderValidationCode.BrokenBranch));
        }
    }
}
