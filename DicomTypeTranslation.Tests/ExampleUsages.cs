using Dicom;
using DicomTypeTranslation.TableCreation;
using FAnsi.Discovery;
using FAnsi.Discovery.TypeTranslation;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace DicomTypeTranslation.Tests
{
    class ExampleUsages
    {
        [Test]
        public void ExampleUsage_Simple()
        {
            //create an Fo-Dicom dataset with a single string
            var ds = new DicomDataset(new List<DicomItem>()
            {
                new DicomShortString(DicomTag.PatientName,"Frank"),
                new DicomAgeString(DicomTag.PatientAge,"032Y"),
                new DicomDate(DicomTag.PatientBirthDate,new DateTime(2001,1,1))
            });

            object name = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientName);
            Assert.AreEqual(typeof(string), name.GetType());
            Assert.AreEqual("Frank", name);

            object age = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientAge);
            Assert.AreEqual(typeof(string), age.GetType());
            Assert.AreEqual("032Y", age);

            object dob = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientBirthDate);
            Assert.AreEqual(typeof(DateTime), dob.GetType());
            Assert.AreEqual(new DateTime(2001, 01, 01), dob);

            //create an Fo-Dicom dataset with string multiplicity
            ds = new DicomDataset(new List<DicomItem>()
            {
                new DicomShortString(DicomTag.PatientName,"Frank","Anderson")
            });

            //Get the C# type
            object name2 = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientName);
            Assert.AreEqual(typeof(string[]), name2.GetType());
            Assert.AreEqual(new string[] { "Frank", "Anderson" }, name2);

            name2 = DicomTypeTranslater.Flatten(name2);
            Assert.AreEqual(typeof(string), name2.GetType());
            Assert.AreEqual("Frank\\Anderson", name2);

            //create an Fo-Dicom dataset with a sequence
            ds = new DicomDataset(new List<DicomItem>()
            {
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID,"1.2.3"),
                new DicomSequence(DicomTag.ActualHumanPerformersSequence,new []
                {
                    new DicomDataset(new List<DicomItem>()
                    {
                        new DicomShortString(DicomTag.PatientName,"Rabbit")
                    }),
                    new DicomDataset(new List<DicomItem>()
                    {
                        new DicomShortString(DicomTag.PatientName,"Roger")
                    })
                })
            });

            var seq = (Dictionary<DicomTag, object>[])DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.ActualHumanPerformersSequence);
            Assert.AreEqual("Rabbit", seq[0][DicomTag.PatientName]);
            Assert.AreEqual("Roger", seq[1][DicomTag.PatientName]);

            var flattened = (string)DicomTypeTranslater.Flatten(seq);
            Assert.AreEqual(
@"[0] - 
 	 (0010,0010) - 	 Rabbit

 [1] - 
 	 (0010,0010) - 	 Roger".Replace("\r", ""), flattened.Replace("\r", ""));
        }

        [Test]
        public void ExampleUsage_Types()
        {
            var tag = DicomDictionary.Default["PatientAddress"];

            DatabaseTypeRequest type = DicomTypeTranslater.GetNaturalTypeForVr(tag.DictionaryEntry.ValueRepresentations, tag.DictionaryEntry.ValueMultiplicity);

            Assert.AreEqual(typeof(string), type.CSharpType);
            Assert.AreEqual(64, type.MaxWidthForStrings);

            TypeTranslater tt = new MicrosoftSQLTypeTranslater();
            Assert.AreEqual("varchar(64)", tt.GetSQLDBTypeForCSharpType(type));

            tt = new OracleTypeTranslater();
            Assert.AreEqual("varchar2(64)", tt.GetSQLDBTypeForCSharpType(type));

        }

        [Ignore("Only works if you have connection string matches an existing server and you have a database called 'test'")]
        [Test]
        public void WorkedExampleTest()
        {
            //pick some tags that we are interested in (determines the table schema created)
            var toCreate = new ImageTableTemplate()
            {
                Columns = new[]{
                    new ImageColumnTemplate(DicomTag.SOPInstanceUID),
                    new ImageColumnTemplate(DicomTag.Modality){AllowNulls = true },
                    new ImageColumnTemplate(DicomTag.PatientID){AllowNulls = true }
                    }
            };

            //load the Sql Server implementation of FAnsi
            ImplementationManager.Load<MicrosoftSQLImplementation>();

            //decide where you want to create the table
            var server = new DiscoveredServer(@"Server=localhost\sqlexpress;Database=mydb;Integrated Security=true;", FAnsi.DatabaseType.MicrosoftSQLServer);
            var db = server.ExpectDatabase("test");

            //create the table
            var tbl = db.CreateTable("MyCoolTable", toCreate.GetColumns(FAnsi.DatabaseType.MicrosoftSQLServer));

            //add a column for where the image is on disk
            tbl.AddColumn("FileLocation", new DatabaseTypeRequest(typeof(string), 500), true, 500);

            //Create a DataTable in memory for the data we read from disk
            DataTable dt = new DataTable();
            dt.Columns.Add("SOPInstanceUID");
            dt.Columns.Add("Modality");
            dt.Columns.Add("PatientID");
            dt.Columns.Add("FileLocation");

            //Load some dicom files and copy tag data into DataTable (where tag exists)
            foreach (string file in Directory.EnumerateFiles(@"C:\temp\TestDicomFiles", "*.dcm", SearchOption.AllDirectories))
            {
                var dcm = DicomFile.Open(file);
                var ds = dcm.Dataset;

                dt.Rows.Add(

                    DicomTypeTranslaterReader.GetCSharpValue(dcm.Dataset, DicomTag.SOPInstanceUID),
                    ds.Contains(DicomTag.Modality) ? DicomTypeTranslaterReader.GetCSharpValue(dcm.Dataset, DicomTag.Modality) : DBNull.Value,
                    ds.Contains(DicomTag.PatientID) ? DicomTypeTranslaterReader.GetCSharpValue(dcm.Dataset, DicomTag.PatientID) : DBNull.Value,
                    file);
            }

            //put the DataTable into the database
            using (var insert = tbl.BeginBulkInsert())
                insert.Upload(dt);
        }

        [Test]
        public void ExampleTableCreation()
        {
            var toCreate = new ImageTableTemplate()
            {
                Columns = new[]{ 
                    
                    //pick some tags for the schema
                    new ImageColumnTemplate(DicomTag.SOPInstanceUID){IsPrimaryKey = true, AllowNulls = false },
                    new ImageColumnTemplate(DicomTag.PatientAge){AllowNulls=true},
                    new ImageColumnTemplate(DicomTag.PatientBirthDate){AllowNulls=true}
                    }
            };

            //load the Sql Server implementation of FAnsi
            ImplementationManager.Load<MicrosoftSQLImplementation>();

            //decide where you want to create the table (these methods will actually attempt to connect to the database)
            var server = new DiscoveredServer("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;", FAnsi.DatabaseType.MicrosoftSQLServer);
            var db = server.ExpectDatabase("MyDb");

            var creator = new ImagingTableCreation(db.Server.GetQuerySyntaxHelper());
            var sql = creator.GetCreateTableSql(db, "MyCoolTable", toCreate);

            //the following Sql gets created
            Assert.AreEqual(
@"CREATE TABLE [MyDb]..[MyCoolTable](
[SOPInstanceUID] varchar(64)    NOT NULL ,
[PatientAge] varchar(4)    NULL ,
[PatientBirthDate] datetime2    NULL ,
 CONSTRAINT PK_MyCoolTable PRIMARY KEY ([SOPInstanceUID]))
"

.Replace("\r", ""), sql.Replace("\r", ""));

            //actually do it
            //creator.CreateTable(db.ExpectTable("MyCoolTable"));
        }

        [Test]
        public void TestGetDataTable()
        {
            //create an Fo-Dicom dataset
            var ds = new DicomDataset(new List<DicomItem>()
            {
                new DicomShortString(DicomTag.PatientName,"Frank"),
                new DicomAgeString(DicomTag.PatientAge,"032Y"),
                new DicomDate(DicomTag.PatientBirthDate,new DateTime(2001,1,1))
            });

            var dt = new DataTable();
            var row = ds.ToRow(dt);

            Assert.AreEqual("Frank", row["PatientName"]);
            Assert.AreEqual("032Y", row["PatientAge"]);
            Assert.AreEqual(new DateTime(2001, 1, 1), row["PatientBirthDate"]);

            //load the MySql implementation of FAnsi
            ImplementationManager.Load<MySqlImplementation>();

            //pick the location of the destination table (must exist, see ExampleTableCreation for how to create)
            var server = new DiscoveredServer(new MySqlConnectionStringBuilder("Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"));
            var table = server.ExpectDatabase("MyDb").ExpectTable("MyCoolTable");

            /*          using(IBulkCopy bulkInsert = table.BeginBulkInsert())
                        {
                            bulkInsert.Upload(dt);
                        }*/


        }
    }

}
