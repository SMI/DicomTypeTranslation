using FellowOakDicom;
using DicomTypeTranslation.Elevation.Serialization;
using DicomTypeTranslation.TableCreation;
using FAnsi.Discovery;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TypeGuesser;

namespace DicomTypeTranslation.Tests;

class TemplateTests:DatabaseTests
{
    [OneTimeSetUp]
    public void DisableDicomValidation()
    {
        DicomValidationBuilderExtension.SkipValidation(null);
    }

    [Test]
    public void Template_ExampleYaml()
    {
        var collection = new ImageTableTemplateCollection();
        var table = new ImageTableTemplate();

        var colTemplate = new ImageColumnTemplate
        {
            ColumnName = "mycol",
            AllowNulls = true,
            Type = new DatabaseTypeRequest(typeof(string),100)
        };

        table.Columns = new[] {colTemplate};
        collection.Tables.Add(table);

        TestContext.Write(collection.Serialize());
    }

    [TestCase("CT", FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("CT", FAnsi.DatabaseType.MySql)]
    [TestCase("CT", FAnsi.DatabaseType.Oracle)]
    [TestCase("MR", FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("MR", FAnsi.DatabaseType.MySql)]
    [TestCase("MR", FAnsi.DatabaseType.Oracle)]
    [TestCase("PT", FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("PT", FAnsi.DatabaseType.MySql)]
    [TestCase("PT", FAnsi.DatabaseType.Oracle)]
    [TestCase("NM", FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("NM", FAnsi.DatabaseType.MySql)]
    [TestCase("NM", FAnsi.DatabaseType.Oracle)]
    [TestCase("OTHER",FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("OTHER",FAnsi.DatabaseType.MySql)]
    [TestCase("OTHER",FAnsi.DatabaseType.Oracle)]
    [TestCase("DX",FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("DX",FAnsi.DatabaseType.MySql)]
    [TestCase("DX",FAnsi.DatabaseType.Oracle)]
    [TestCase("SR",FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("SR",FAnsi.DatabaseType.MySql)]
    [TestCase("SR",FAnsi.DatabaseType.Oracle)]
    [TestCase("ECG",FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("ECG",FAnsi.DatabaseType.MySql)]
    [TestCase("ECG",FAnsi.DatabaseType.Oracle)]
    [TestCase("XA", FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("XA", FAnsi.DatabaseType.MySql)]
    [TestCase("XA", FAnsi.DatabaseType.Oracle)]
    [TestCase("US", FAnsi.DatabaseType.MicrosoftSQLServer)]
    [TestCase("US", FAnsi.DatabaseType.MySql)]
    [TestCase("US", FAnsi.DatabaseType.Oracle)]
    public void TestTemplate(string template, FAnsi.DatabaseType dbType)
    {
        var templateFile = Path.Combine(TestContext.CurrentContext.TestDirectory,"Templates", $"{template}.it");

        var collection = ImageTableTemplateCollection.LoadFrom(File.ReadAllText(templateFile));

        foreach (var tableTemplate in collection.Tables)
            Validate(tableTemplate,templateFile);

        var db = GetTestDatabase(dbType);

        var creator = new ImagingTableCreation(db.Server.GetQuerySyntaxHelper());

        foreach(var table in collection.Tables)
        {
            if(string.Equals(table.TableName, "ImageTable",StringComparison.CurrentCultureIgnoreCase))
            {
                EnforceExpectedImageColumns(template,table);
            }

            var tbl = db.ExpectTable(table.TableName);
            creator.CreateTable(tbl,table);

            Assert.That(tbl.Exists(), Is.True);
        }
    }

    private void EnforceExpectedImageColumns(string template, ImageTableTemplate table)
    {
        foreach(var req in new[] { "PatientID","DicomFileSize","StudyInstanceUID"})
        {
            if (!table.Columns.Any(c => c.ColumnName.Equals(req)))
            {
                Assert.Fail($"Template {Path.GetFileName(template)} is missing expected field {req} in section {table.TableName}");
            }
        }

    }

    private void Validate(ImageTableTemplate tableTemplate, string templateFile)
    {
        var errors = new List<Exception>();

        foreach (var col in tableTemplate.Columns)
        {
            try
            {
                Assert.That(col.ColumnName, Has.Length.LessThanOrEqualTo(64), $"Column name '{col.ColumnName}' is too long");

                var rSeq = new Regex(@"_([A-Za-z]+)$");
                var seqMatch = rSeq.Match(col.ColumnName);

                if (seqMatch.Success)
                {
                    var leafTag = seqMatch.Groups[1].Value;

                    var tag = DicomDictionary.Default.FirstOrDefault(t => t.Keyword == leafTag) ?? throw new NotSupportedException($"Leaf tag {leafTag} of sequence column {col.ColumnName} was not a valid dicom tag name");
                    var type = DicomTypeTranslater.GetNaturalTypeForVr(tag.ValueRepresentations, tag.ValueMultiplicity);

                    Assert.Multiple(() =>
                    {
                        Assert.That(col.Type.CSharpType, Is.EqualTo(type.CSharpType), $"Listed Type for column {col.ColumnName} did not match expected Type");

                        // The declared widths must be sufficient to hold the basic leaf node
                        Assert.That(col.Type.Width ?? 0,
                            type.Width == int.MaxValue
                                ? Is.GreaterThanOrEqualTo(100)
                                : Is.GreaterThanOrEqualTo(type.Width ?? 0),
                            $"Listed Width for column {col.ColumnName} did not match expected minimum Width");

                        Assert.That(col.Type.Size, Is.EqualTo(type.Size), $"Listed Size for column {col.ColumnName} ({DescribeSize(col.Type.Size)}) did not match expected Size ({DescribeSize(type.Size)})");
                    });

                }
            }
            catch (Exception e)
            {
                errors.Add(e);
            }
        }

        if(errors.Any())
            throw new AggregateException($"Errors in file '{templateFile}'",errors.ToArray());
    }

    private string DescribeSize(DecimalSize typeSize)
    {
        return
            $"NumbersBeforeDecimalPlace:{typeSize.NumbersBeforeDecimalPlace} NumbersAfterDecimalPlace:{typeSize.NumbersAfterDecimalPlace}";
    }

    [TestCase("SmiTagElevation")]
    public void TestElevationTemplate(string template)
    {
        var templateFile = Path.Combine(TestContext.CurrentContext.TestDirectory,"Templates", $"{template}.xml");

        var elevation = new TagElevationRequestCollection(File.ReadAllText(templateFile));

        //at least one request
        Assert.That(elevation.Requests, Is.Not.Empty);

    }

    /// <summary>
    /// Tests the systems ability to serialize and deserialize <see cref="ImageTableTemplateCollection"/> which has only
    /// columns with DicomTags in them
    /// </summary>
    [Test]
    public void Test_Serializing_DicomTagsOnly()
    {
        var collection = new ImageTableTemplateCollection();

        var table = new ImageTableTemplate
        {
            TableName = "Fish",
            Columns = new[]
            {
                new ImageColumnTemplate(DicomTag.SOPInstanceUID){IsPrimaryKey = true },
                new ImageColumnTemplate(DicomTag.CurrentPatientLocation)
            }
        };


        collection.Tables.Add(table);

        var yaml = collection.Serialize();

        var collection2 = ImageTableTemplateCollection.LoadFrom(yaml);

        Assert.Multiple(() =>
        {
            Assert.That(collection2.Tables[0].TableName, Is.EqualTo(collection.Tables[0].TableName));

            Assert.That(collection2.Tables[0].Columns[0].ColumnName, Is.EqualTo(collection.Tables[0].Columns[0].ColumnName));
            Assert.That(collection2.Tables[0].Columns[0].Type, Is.EqualTo(collection.Tables[0].Columns[0].Type));
            Assert.That(collection2.Tables[0].Columns[0].IsPrimaryKey, Is.EqualTo(collection.Tables[0].Columns[0].IsPrimaryKey));

            Assert.That(collection2.Tables[0].Columns[1].ColumnName, Is.EqualTo(collection.Tables[0].Columns[1].ColumnName));
            Assert.That(collection2.Tables[0].Columns[1].Type, Is.EqualTo(collection.Tables[0].Columns[1].Type));
            Assert.That(collection2.Tables[0].Columns[1].IsPrimaryKey, Is.EqualTo(collection.Tables[0].Columns[1].IsPrimaryKey));
        });

        //doesn't actually have to exist
        var db = new DiscoveredServer("localhost","nobody",FAnsi.DatabaseType.MySql,"captain","morgans").ExpectDatabase("neverland");

        var creator = new ImagingTableCreation(db.Server.GetQuerySyntaxHelper());


        var sql1 = creator.GetCreateTableSql(db,collection.Tables[0].TableName, collection.Tables[0],null);
        var sql2 = creator.GetCreateTableSql(db, collection2.Tables[0].TableName, collection.Tables[0], null);

        Assert.That(sql2, Is.EqualTo(sql1));
    }

    /// <summary>
    /// Tests the systems ability to serialize and deserialize <see cref="ImageTableTemplateCollection"/> when there
    /// is a mixture of DicomTag and normal columns
    /// </summary>
    [Test]
    public void Test_Serializing_DicomTagsAndArbitraryColumns()
    {
        var collection = new ImageTableTemplateCollection();

        var table = new ImageTableTemplate
        {
            TableName = "Fish",
            //table has 3 columns, one is a DicomTag (SOPInstanceUID) while the other 2 are arbitrary
            Columns = new[]
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
            }
        };


        collection.Tables.Add(table);

        var yaml = collection.Serialize();

        Console.WriteLine("Yaml is:");
        Console.Write(yaml);

        var collection2 = ImageTableTemplateCollection.LoadFrom(yaml);

        Assert.Multiple(() =>
        {
            Assert.That(collection2.Tables[0].TableName, Is.EqualTo(collection.Tables[0].TableName));

            Assert.That(collection2.Tables[0].Columns[0].ColumnName, Is.EqualTo(collection.Tables[0].Columns[0].ColumnName));
            Assert.That(collection2.Tables[0].Columns[0].Type, Is.EqualTo(collection.Tables[0].Columns[0].Type));
            Assert.That(collection2.Tables[0].Columns[0].IsPrimaryKey, Is.EqualTo(collection.Tables[0].Columns[0].IsPrimaryKey));

            Assert.That(collection2.Tables[0].Columns[4].ColumnName, Is.EqualTo(collection.Tables[0].Columns[4].ColumnName));
            Assert.That(collection2.Tables[0].Columns[4].Type, Is.EqualTo(collection.Tables[0].Columns[4].Type));
            Assert.That(collection2.Tables[0].Columns[4].IsPrimaryKey, Is.EqualTo(collection.Tables[0].Columns[4].IsPrimaryKey));

            Assert.That(collection2.Tables[0].Columns[5].ColumnName, Is.EqualTo(collection.Tables[0].Columns[5].ColumnName));
            Assert.That(collection2.Tables[0].Columns[5].Type, Is.EqualTo(collection.Tables[0].Columns[5].Type));
            Assert.That(collection2.Tables[0].Columns[5].IsPrimaryKey, Is.EqualTo(collection.Tables[0].Columns[5].IsPrimaryKey));
        });

        //doesn't actually have to exist
        var db = new DiscoveredServer("localhost", "nobody", FAnsi.DatabaseType.MySql, "captain", "morgans").ExpectDatabase("neverland");

        var creator = new ImagingTableCreation(db.Server.GetQuerySyntaxHelper());

        var sql1 = creator.GetCreateTableSql(db, collection.Tables[0].TableName, collection.Tables[0], null);
        var sql2 = creator.GetCreateTableSql(db, collection2.Tables[0].TableName, collection.Tables[0], null);

        Assert.That(sql2, Is.EqualTo(sql1));
    }

}