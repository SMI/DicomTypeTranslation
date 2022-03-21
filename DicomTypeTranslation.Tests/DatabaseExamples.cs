using FellowOakDicom;
using DicomTypeTranslation.TableCreation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using TypeGuesser;

namespace DicomTypeTranslation.Tests
{
    public class DatabaseExamples : DatabaseTests
    {
        [TestCase(FAnsi.DatabaseType.MicrosoftSQLServer)]
        [TestCase(FAnsi.DatabaseType.MySql)]
        [TestCase(FAnsi.DatabaseType.Oracle)]
        public void WorkedExampleTest(FAnsi.DatabaseType dbType)
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


            //decide where you want to create the table
            var db = GetTestDatabase(dbType);

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

            var dir = Path.Combine(TestContext.CurrentContext.TestDirectory ,"TestDicomFiles");

            //Load some dicom files and copy tag data into DataTable (where tag exists)
            foreach (string file in Directory.EnumerateFiles(dir, "*.dcm", SearchOption.AllDirectories))
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

        [TestCase(FAnsi.DatabaseType.MicrosoftSQLServer)]
        [TestCase(FAnsi.DatabaseType.MySql)]
        [TestCase(FAnsi.DatabaseType.Oracle)]
        public void ExampleTableCreation(FAnsi.DatabaseType dbType)
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
                        
            //decide where you want to create the table
            var db = GetTestDatabase(dbType);

            var creator = new ImagingTableCreation(db.Server.GetQuerySyntaxHelper());

            //actually do it
            creator.CreateTable(db.ExpectTable("MyCoolTable"),toCreate);
        }

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
        }
    }
}
