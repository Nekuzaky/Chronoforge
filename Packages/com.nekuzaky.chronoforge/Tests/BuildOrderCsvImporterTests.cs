using System.Collections.Generic;
using NUnit.Framework;

namespace Chronoforge.Tests
{
    public sealed class BuildOrderCsvImporterTests
    {
        [Test]
        public void Parse_WithHeader_MapsColumnsByName()
        {
            const string csv = "supply,time,type,title\n12,1:00,Unit,Zealot";
            List<BuildOrderStep> steps = BuildOrderCsvImporter.Parse(csv, out string error);

            Assert.IsEmpty(error);
            Assert.AreEqual(1, steps.Count);
            Assert.AreEqual(12, steps[0].m_Supply);
            Assert.AreEqual(60f, steps[0].m_TimeSeconds);
            Assert.AreEqual(BuildOrderActionType.Unit, steps[0].m_Type);
            Assert.AreEqual("Zealot", steps[0].m_Title);
        }

        [Test]
        public void Parse_NoHeader_UsesFixedColumnOrder()
        {
            const string csv = "13,0:20,Building,Gateway";
            List<BuildOrderStep> steps = BuildOrderCsvImporter.Parse(csv, out _);

            Assert.AreEqual(1, steps.Count);
            Assert.AreEqual(BuildOrderActionType.Building, steps[0].m_Type);
            Assert.AreEqual("Gateway", steps[0].m_Title);
        }

        [Test]
        public void Parse_NumericType_FallsBackToCustom()
        {
            const string csv = "12,1:00,3,Thing";
            List<BuildOrderStep> steps = BuildOrderCsvImporter.Parse(csv, out _);

            Assert.AreEqual(BuildOrderActionType.Custom, steps[0].m_Type);
        }

        [Test]
        public void Parse_SemicolonDelimiter_IsDetected()
        {
            const string csv = "12;1:00;Unit;Marine";
            List<BuildOrderStep> steps = BuildOrderCsvImporter.Parse(csv, out _);

            Assert.AreEqual(1, steps.Count);
            Assert.AreEqual("Marine", steps[0].m_Title);
        }

        [Test]
        public void Parse_Empty_ReturnsError()
        {
            List<BuildOrderStep> steps = BuildOrderCsvImporter.Parse("", out string error);

            Assert.AreEqual(0, steps.Count);
            Assert.IsNotEmpty(error);
        }
    }
}
