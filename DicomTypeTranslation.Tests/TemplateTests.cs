using Dicom;
using DicomTypeTranslation.Elevation;
using DicomTypeTranslation.Elevation.Serialization;
using DicomTypeTranslation.TableCreation;
using FAnsi.Discovery;
using FAnsi.Discovery.TypeTranslation;
using NUnit.Framework;
using System;
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

        /// <summary>
        /// Tests the systems ability to serialize and deserialize <see cref="ImageTableTemplateCollection"/> which has only
        /// columns with DicomTags in them
        /// </summary>
        [Test]
        public void Test_Serializing_DicomTagsOnly()
        {
            ImageTableTemplateCollection collection = new ImageTableTemplateCollection();

            ImageTableTemplate table = new ImageTableTemplate();
            table.TableName = "Fish";
            
            
            table.Columns = new[]
            {
                new ImageColumnTemplate(DicomTag.SOPInstanceUID){IsPrimaryKey = true },
                new ImageColumnTemplate(DicomTag.CurrentPatientLocation)
            };

            collection.Tables.Add(table);

            var yaml = collection.Serialize();

            var collection2 = ImageTableTemplateCollection.LoadFrom(yaml);

            Assert.AreEqual(collection.Tables[0].TableName, collection2.Tables[0].TableName);

            Assert.AreEqual(collection.Tables[0].Columns[0].ColumnName,   collection2.Tables[0].Columns[0].ColumnName );
            Assert.AreEqual(collection.Tables[0].Columns[0].Type, collection2.Tables[0].Columns[0].Type);
            Assert.AreEqual(collection.Tables[0].Columns[0].IsPrimaryKey, collection2.Tables[0].Columns[0].IsPrimaryKey);

            Assert.AreEqual(collection.Tables[0].Columns[1].ColumnName,   collection2.Tables[0].Columns[1].ColumnName);
            Assert.AreEqual(collection.Tables[0].Columns[1].Type, collection2.Tables[0].Columns[1].Type);
            Assert.AreEqual(collection.Tables[0].Columns[1].IsPrimaryKey, collection2.Tables[0].Columns[1].IsPrimaryKey);

            //doesn't actually have to exist
            var db = new DiscoveredServer("localhost","nobody",FAnsi.DatabaseType.MySql,"captain","morgans").ExpectDatabase("neverland");

            ImagingTableCreation creator = new ImagingTableCreation(db.Server.GetQuerySyntaxHelper());
                       

            var sql1 = creator.GetCreateTableSql(db,collection.Tables[0].TableName, collection.Tables[0],null);
            var sql2 = creator.GetCreateTableSql(db, collection2.Tables[0].TableName, collection.Tables[0], null);

            Assert.AreEqual(sql1,sql2);
        }
        
        /// <summary>
        /// Tests the systems ability to serialize and deserialize <see cref="ImageTableTemplateCollection"/> when there
        /// is a mixture of DicomTag and normal columns
        /// </summary>
        [Test]
        public void Test_Serializing_DicomTagsAndArbitraryColumns()
        {
            ImageTableTemplateCollection collection = new ImageTableTemplateCollection();

            ImageTableTemplate table = new ImageTableTemplate();
            table.TableName = "Fish";

            //table has 3 columns, one is a DicomTag (SOPInstanceUID) while the other 2 are arbitrary
            table.Columns = new[]
            {
                new ImageColumnTemplate(DicomTag.SOPInstanceUID){IsPrimaryKey = true },
                new ImageColumnTemplate(DicomTag.SeriesInstanceUID),
                new ImageColumnTemplate(DicomTag.StudyInstanceUID),
                new ImageColumnTemplate(DicomTag.PatientID),
                new ImageColumnTemplate(){
                    ColumnName = "LocationOfFiles",
                    Type = new DatabaseTypeRequest(typeof(string),500,null),
                    AllowNulls = true},
                new ImageColumnTemplate(){
                    ColumnName = "DataQualityEngineScore",
                    Type = new DatabaseTypeRequest(typeof(decimal),null,new DecimalSize(10,5)),
                    AllowNulls = false} 
            };
    

            collection.Tables.Add(table);

            var yaml = collection.Serialize();

            Console.WriteLine("Yaml is:");
            Console.Write(yaml);

            var collection2 = ImageTableTemplateCollection.LoadFrom(yaml);

            Assert.AreEqual(collection.Tables[0].TableName, collection2.Tables[0].TableName);

            Assert.AreEqual(collection.Tables[0].Columns[0].ColumnName,     collection2.Tables[0].Columns[0].ColumnName);
            Assert.AreEqual(collection.Tables[0].Columns[0].Type,           collection2.Tables[0].Columns[0].Type);
            Assert.AreEqual(collection.Tables[0].Columns[0].IsPrimaryKey,   collection2.Tables[0].Columns[0].IsPrimaryKey);

            Assert.AreEqual(collection.Tables[0].Columns[4].ColumnName,     collection2.Tables[0].Columns[4].ColumnName);
            Assert.AreEqual(collection.Tables[0].Columns[4].Type,           collection2.Tables[0].Columns[4].Type);
            Assert.AreEqual(collection.Tables[0].Columns[4].IsPrimaryKey,   collection2.Tables[0].Columns[4].IsPrimaryKey);
            
            Assert.AreEqual(collection.Tables[0].Columns[5].ColumnName,     collection2.Tables[0].Columns[5].ColumnName);
            Assert.AreEqual(collection.Tables[0].Columns[5].Type,           collection2.Tables[0].Columns[5].Type);
            Assert.AreEqual(collection.Tables[0].Columns[5].IsPrimaryKey,   collection2.Tables[0].Columns[5].IsPrimaryKey);

            //doesn't actually have to exist
            var db = new DiscoveredServer("localhost", "nobody", FAnsi.DatabaseType.MySql, "captain", "morgans").ExpectDatabase("neverland");

            ImagingTableCreation creator = new ImagingTableCreation(db.Server.GetQuerySyntaxHelper());

            var sql1 = creator.GetCreateTableSql(db, collection.Tables[0].TableName, collection.Tables[0], null);
            var sql2 = creator.GetCreateTableSql(db, collection2.Tables[0].TableName, collection.Tables[0], null);

            Assert.AreEqual(sql1, sql2);
        }

    }
}

