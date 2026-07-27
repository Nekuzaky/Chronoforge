using NUnit.Framework;
using static Chronoforge.Tests.BuildOrderTestFactory;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderCleanBuildTests
    {
        private static BuildOrderAsset EnabledAsset()
        {
            BuildOrderAsset asset = Asset();
            asset.m_CleanBuild.m_Enabled = true;
            asset.m_CleanBuild.m_StartingSupplyCap = 10;
            return asset;
        }

        [Test]
        public void Disabled_ProducesNoExecutionIssues()
        {
            BuildOrderAsset asset = Asset();
            BuildOrderStep step = Step("a", "Marine", delta: 50);
            asset.m_Steps.Add(step);

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsFalse(HasCode(result.m_Issues, BuildOrderValidationCode.SupplyBlock));
        }

        [Test]
        public void SupplyBlock_IsFlaggedWhenCapExceeded()
        {
            BuildOrderAsset asset = EnabledAsset();
            asset.m_Steps.Add(Step("a", "Marine", time: 0, delta: 8));
            asset.m_Steps.Add(Step("b", "Marine", time: 20, delta: 8));   // 16 > cap 10

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsTrue(HasCode(result.m_Issues, BuildOrderValidationCode.SupplyBlock));
        }

        [Test]
        public void SupplyBlock_IsNotFlaggedWhenSupplyProvidedInTime()
        {
            BuildOrderAsset asset = EnabledAsset();
            asset.m_Steps.Add(Step("a", "Marine", time: 0, delta: 8));
            BuildOrderStep depot = Step("d", "Depot", time: 10);
            depot.m_SupplyProvided = 8;
            asset.m_Steps.Add(depot);
            asset.m_Steps.Add(Step("b", "Marine", time: 20, delta: 8));   // 16 <= cap 18

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsFalse(HasCode(result.m_Issues, BuildOrderValidationCode.SupplyBlock));
        }

        [Test]
        public void WorkerGap_IsFlaggedWhenProductionPauses()
        {
            BuildOrderAsset asset = EnabledAsset();
            asset.m_CleanBuild.m_MaxWorkerGapSeconds = 15f;

            BuildOrderStep first = Step("w1", "Worker", time: 0);
            first.m_IsWorker = true;
            BuildOrderStep second = Step("w2", "Worker", time: 60);
            second.m_IsWorker = true;
            asset.m_Steps.Add(first);
            asset.m_Steps.Add(second);

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsTrue(HasCode(result.m_Issues, BuildOrderValidationCode.WorkerProductionGap));
        }

        [Test]
        public void IdleProduction_IsFlaggedWhenFacilitySitsUnused()
        {
            BuildOrderAsset asset = EnabledAsset();
            asset.m_CleanBuild.m_MaxFacilityIdleSeconds = 10f;

            BuildOrderStep barracks = Step("b", "Barracks", time: 0, type: BuildOrderActionType.Building);
            barracks.m_ProvidesFacilityId = "barracks";
            BuildOrderStep marine = Step("m", "Marine", time: 90);
            marine.m_ProducedByFacilityId = "barracks";
            asset.m_Steps.Add(barracks);
            asset.m_Steps.Add(marine);

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsTrue(HasCode(result.m_Issues, BuildOrderValidationCode.IdleProduction));
        }

        [Test]
        public void ProducedByUnknownFacility_IsAnError()
        {
            BuildOrderAsset asset = EnabledAsset();
            BuildOrderStep marine = Step("m", "Marine", time: 10);
            marine.m_ProducedByFacilityId = "ghost-facility";
            asset.m_Steps.Add(marine);

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsTrue(HasCode(result.m_Issues, BuildOrderValidationCode.IdleProduction));
            Assert.GreaterOrEqual(result.ErrorCount, 1, "unknown facility must raise an error");
        }

        [Test]
        public void Stockpiling_IsFlaggedWhenIncomeGoesUnspent()
        {
            BuildOrderAsset asset = EnabledAsset();
            asset.m_CleanBuild.m_StockpileThreshold = 300f;
            asset.m_ResourceModel.Add(new BuildOrderResourceRate
            {
                m_ResourceId = "minerals",
                m_StartingAmount = 50f,
                m_IncomePerSecond = 2f
            });
            asset.m_Steps.Add(Step("a", "Worker", time: 0));
            asset.m_Steps.Add(Step("b", "Nothing", time: 300));   // 50 + 600 unspent

            BuildOrderEvaluationResult result = BuildOrderEvaluation.Run(asset);

            Assert.IsTrue(HasCode(result.m_Issues, BuildOrderValidationCode.ResourceStockpiling));
        }
    }
}
