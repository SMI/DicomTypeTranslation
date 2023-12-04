
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FellowOakDicom;
using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.Helpers;
using NLog;
using NUnit.Framework;

namespace DicomTypeTranslation.Tests;

[TestFixture]
public class DicomTypeTranslatorTests
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    #region Fixture Methods

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestLogger.Setup();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        TestLogger.ShutDown();
    }

    #endregion

    #region Tests

    [Test]
    public void TestBasicCSharpTranslation()
    {
        var ds = TranslationTestHelpers.BuildVrDataset();

        foreach (var item in ds)
            Assert.That(DicomTypeTranslaterReader.GetCSharpValue(ds, item), Is.Not.Null);
    }

    [Test]
    public void TestSequenceConversion()
    {
        var subDataset = new DicomDataset
        {
            new DicomShortString(DicomTag.SpecimenShortDescription, "short desc"),
            // Note JS 2022-03-18: 3 digit ages only
            new DicomAgeString(DicomTag.PatientAge, "099Y")
        };

        var ds = new DicomDataset
        {
            new DicomSequence(DicomTag.ReferencedImageSequence, subDataset)
        };

        var obj = DicomTypeTranslaterReader.GetCSharpValue(ds, ds.GetDicomItem<DicomItem>(DicomTag.ReferencedImageSequence));

        var asArray = obj as Dictionary<DicomTag, object>[];
        Assert.That(asArray, Is.Not.Null);

        Assert.That(asArray, Has.Length.EqualTo(1));
        Assert.That(asArray[0], Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(asArray[0][DicomTag.SpecimenShortDescription], Is.EqualTo("short desc"));
            Assert.That(asArray[0][DicomTag.PatientAge], Is.EqualTo("099Y"));
        });
    }

    [Test]
    public void TestWriteMultiplicity()
    {
        var stringMultiTag = DicomTag.SpecimenShortDescription;
        string[] values = { "this", "is", "a", "multi", "element", "" };

        var ds = new DicomDataset();

        DicomTypeTranslaterWriter.SetDicomTag(ds, stringMultiTag, values);

        Assert.Multiple(() =>
        {
            Assert.That(ds.Count(), Is.EqualTo(1));
            Assert.That(ds.GetDicomItem<DicomElement>(stringMultiTag).Count, Is.EqualTo(6));
            Assert.That(ds.GetString(stringMultiTag), Is.EqualTo("this\\is\\a\\multi\\element\\"));
        });
    }

    [Test]
    public void TestMultipleElementSequences()
    {
        var subDatasets = new List<DicomDataset>();
        for (var i = 0; i < 3; i++)
        {
            subDatasets.Add(new DicomDataset
            {
                // Hemodynamic Waveform Storage class UID, plus counter
                {DicomTag.ReferencedSOPClassUID, $"1.2.840.10008.5.1.4.1.1.9.2.1.{(i + 1)}" },
                // Truncated example instance UID from dicom.innolytics.com, plus counter
                {DicomTag.ReferencedSOPInstanceUID, $"1.3.6.1.4.1.14519.5.2.1.7695.2311.916784049.{(i + 1)}" }
            });
        }

        var originalDataset = new DicomDataset
        {
            {DicomTag.ReferencedImageSequence, subDatasets.ToArray()}
        };

        var translatedDataset = new Dictionary<DicomTag, object>();

        foreach (var item in originalDataset)
        {
            var value = DicomTypeTranslaterReader.GetCSharpValue(originalDataset, item);
            translatedDataset.Add(item.Tag, value);
        }

        var reconstructedDataset = new DicomDataset();

        foreach (var item in translatedDataset)
            DicomTypeTranslaterWriter.SetDicomTag(reconstructedDataset, item.Key, item.Value);

        Assert.That(DicomDatasetHelpers.ValueEquals(originalDataset, reconstructedDataset), Is.True);
    }

    [Test]
    public void TestSetDicomTagWithNullElement()
    {
        var dataset = new DicomDataset();

        // Test with a string element and a value element
        var asTag = DicomTag.SelectorASValue;
        var flTag = DicomTag.SelectorFLValue;

        DicomTypeTranslaterWriter.SetDicomTag(dataset, asTag, null);
        DicomTypeTranslaterWriter.SetDicomTag(dataset, flTag, null);

        Assert.That(dataset.Count(), Is.EqualTo(2));

        var asElement = dataset.GetDicomItem<DicomElement>(DicomTag.SelectorASValue);
        Assert.That(asElement.Buffer.Size, Is.EqualTo(0));

        var flElement = dataset.GetDicomItem<DicomElement>(DicomTag.SelectorFLValue);
        Assert.That(flElement.Buffer.Size, Is.EqualTo(0));
    }

    [Test]
    public void Test_Sequence()
    {
        var subDatasets = new List<DicomDataset>
        {
            new DicomDataset
            {
                new DicomShortString(DicomTag.CodeValue, "CPELVD")
            }
        };

        var dicomDataset = new DicomDataset
        {
            {DicomTag.ProcedureCodeSequence, subDatasets.ToArray()}
        };

        var result = DicomTypeTranslaterReader.GetCSharpValue(dicomDataset, DicomTag.ProcedureCodeSequence);


        var flat = DicomTypeTranslater.Flatten(result);
        Console.WriteLine(flat);

        Assert.That((string)flat, Does.Contain("CPELVD"));
        Assert.That((string)flat, Does.Contain("(0008,0100)"));
    }

    [Test]
    public void TestPatientAgeTag()
    {
        var dataset = new DicomDataset();

        dataset.Add(new DicomAgeString(DicomTag.PatientAge, "009Y"));

        var cSharpValue = DicomTypeTranslaterReader.GetCSharpValue(dataset, DicomTag.PatientAge);

        Assert.That(cSharpValue, Is.EqualTo("009Y"));
    }


    [Test]
    public void PrintValueTypesForVrs()
    {
        var vrs = TranslationTestHelpers.AllVrCodes;
        var uniqueTypes = new SortedSet<string>();

        foreach (var vr in vrs)
        {
            //SQ value representation doesn't have ValueType defined
            if (vr == DicomVR.SQ)
                continue;

            _logger.Info($"VR: {vr.Code}\t Type: {vr.ValueType.Name}\t IsString: {vr.IsString}");
            uniqueTypes.Add(vr.ValueType.Name.TrimEnd(']', '['));
        }

        var sb = new StringBuilder();
        foreach (var str in uniqueTypes)
            sb.Append($"{str}, ");

        sb.Length -= 2;
        _logger.Info($"Unique underlying types: {sb}");
    }

    [Test]
    public void TestGetCSharpValueThrowsException()
    {
        Assert.Throws<FellowOakDicom.DicomValidationException>(static () => DicomTypeTranslaterReader.GetCSharpValue(
            new DicomDataset
            {
                new DicomDecimalString(DicomTag.SelectorDSValue, "aaahhhhh")
            },
            DicomTag.SelectorDSValue));
    }


    [Test]
    public void Test_GetCSharpValue_PrivateTags()
    {
        // Create a dataset with the private tag in it
        var aTag = new DicomTag(0x3001, 0x08, "PRIVATE");
        var ds = new DicomDataset();
        ds.Add<int>(DicomVR.IS,aTag, 1);

        Assert.Multiple(() =>
        {
            // Getting the value directly is fine
            Assert.That(ds.GetSingleValue<int>(aTag), Is.EqualTo(1));
            Assert.That(DicomTypeTranslaterReader.GetCSharpValue(ds, aTag), Is.EqualTo(1));
        });

        // Getting it by iterating through the dataset also works
        // NOTE(rkm 2020-03-26) When creating a dataset with private tags, the "Private Creator" tags are also implicitly added to the dataset
        foreach (var item in ds)
            if (item.ToString().Contains("(3001,1008)"))
                Assert.That(DicomTypeTranslaterReader.GetCSharpValue(ds, item), Is.EqualTo(1));
    }

    [Test]
    public void ShowBlacklistedTags()
    {
        var blacklistedTags = typeof(DicomTag)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DicomTag))
            .Select(field => (DicomTag)field.GetValue(null))
            .Where(tag => tag.DictionaryEntry.ValueRepresentations
                .Any(vr => DicomTypeTranslater.DicomVrBlacklist.Contains(vr)))
            .ToList();

        Console.WriteLine($"Total blacklisted elements {blacklistedTags.Count}");

        foreach (var tag in blacklistedTags.OrderBy(x => x.DictionaryEntry.Keyword))
            Console.WriteLine($"{tag} || {tag.DictionaryEntry.Keyword}");
    }

    /// <summary>
    /// This test will fail when new VRs are added by fo-dicom (as part of new DICOM standards). This library needs to handle new VRs in the following places:
    /// - SmiJsonConverter: WriteJsonDicomItem, ReadJsonDicomItem, CreateDicomItem (???)
    /// - DicomTypeTranslater
    /// - DicomTypeTranslaterWriter
    /// - DicomTypeTranslaterReader
    /// - Test code for the above
    /// </summary>
    [Test]
    public void CheckForNewVrs()
    {
        Assert.That(TranslationTestHelpers.AllVrCodes, Has.Length.EqualTo(34));
    }

    [Test]
    public void GetCSharpValue_ExceptionIncludesTag()
    {
        // Arrange
        var tag = DicomTag.NumericValue;
        var ds = new DicomDataset
        {
            new DicomDecimalString(tag, new[] { "3.40282347e+038", "3.0e+038" }),
        };

        // Act
        var exc = Assert.Throws<ArgumentException>(() => DicomTypeTranslaterReader.GetCSharpValue(ds, tag));

        // Assert
        Assert.That(exc.Message, Is.EqualTo(@"Tag NumericValue (0040,a30a) has invalid value(s): '3.40282347e+038\3.0e+038'"));
    }

    #endregion
}