using DicomTypeTranslation.Elevation;
using DicomTypeTranslation.Elevation.Serialization;
using DicomTypeTranslation.TableCreation;
using NUnit.Framework;
using System.IO;

namespace DicomTypeTranslation.Tests
{
    class TemplateTests:DatabaseTests
    {
        [TestCase("CT", FAnsi.DatabaseType.MicrosoftSQLServer)]
        [TestCase("CT", FAnsi.DatabaseType.MySql)]
        [TestCase("CT", FAnsi.DatabaseType.Oracle)]
        [TestCase("MR", FAnsi.DatabaseType.MicrosoftSQLServer)]
        [TestCase("MR", FAnsi.DatabaseType.MySql)]
        [TestCase("MR", FAnsi.DatabaseType.Oracle)]
        [TestCase("OTHER",FAnsi.DatabaseType.MicrosoftSQLServer)]
        [TestCase("OTHER",FAnsi.DatabaseType.MySql)]
        [TestCase("OTHER",FAnsi.DatabaseType.Oracle)]
        public void TestTemplate(string template, FAnsi.DatabaseType dbType)
        {
            string templateFile = Path.Combine(TestContext.CurrentContext.TestDirectory,"Templates",template + ".it");

            ImageTableTemplateCollection collection = ImageTableTemplateCollection.LoadFrom(File.ReadAllText(templateFile));
            
            var db = GetTestDatabase(dbType);
            
            var creator = new ImagingTableCreation(db.Server.GetQuerySyntaxHelper());

            foreach(var table in collection.Tables)
            {
                var tbl = db.ExpectTable(table.TableName);
                creator.CreateTable(tbl,table);

                Assert.IsTrue(tbl.Exists());
            }
        }

        [TestCase("SmiTagElevation")]
        public void TestElevationTemplate(string template)
        {
            string templateFile = Path.Combine(TestContext.CurrentContext.TestDirectory,"Templates",template + ".xml");
            
            TagElevationRequestCollection elevation = new TagElevationRequestCollection(File.ReadAllText(templateFile));
            
            //at least one request
            Assert.GreaterOrEqual(elevation.Requests.Count,1);

        }
    }
}
