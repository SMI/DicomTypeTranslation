
using Dicom;
using Dicom.Serialization;
using DicomTypeTranslation.Converters;
using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.ElevationTests;
using DicomTypeTranslation.Tests.Helpers;
using MongoDB.Bson;
using Newtonsoft.Json;
using NLog;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace DicomTypeTranslation.Tests
{
    [TestFixture(ConverterTestCase.Standard)]
    [TestFixture(ConverterTestCase.SmiStrict)]
    [TestFixture(ConverterTestCase.SmiLazy)]
    public class JsonDicomConverterTests
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly JsonConverter _jsonDicomConverter;

        private readonly string _testDcmPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "testFile.dcm");

        #region Fixture Methods 

        public JsonDicomConverterTests(ConverterTestCase converterTestCase)
        {
            Type converterType;

            switch (converterTestCase)
            {
                case ConverterTestCase.Standard:
                    converterType = typeof(JsonDicomConverter);
                    break;
                case ConverterTestCase.SmiStrict:
                    converterType = typeof(SmiStrictJsonDicomConverter);
                    break;
                case ConverterTestCase.SmiLazy:
                    converterType = typeof(SmiLazyJsonDicomConverter);
                    break;
                default:
                    throw new Exception("No converter for test case " + converterTestCase);
            }

            _jsonDicomConverter = TranslationTestHelpers.GetConverter(converterType);
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            TestLogger.Setup();
        }

        #endregion

        #region Test Helpers

        /// <summary>
        /// Enum to enable selection of each JSON converter
        /// </summary>
        public enum ConverterTestCase
        {
            Standard,   // The standard Json converter used in fo-dicom. Conforms to Dicom specification for Json representation
            SmiStrict,  // Our version of the standard converter, with some extra handling of 'dodgy' data
            SmiLazy     // Our custom version of the converter which just uses the underlying element value types used by fo-dicom
        }

        /// <summary>
        /// Serializes originalDataset to JSON, deserializes, and re-serializes.
        /// Verifies that both datasets are equal, and both json serializations are equal!
        /// </summary>
        private void VerifyJsonTripleTrip(DicomDataset originalDataset)
        {
            string json = DicomTypeTranslater.SerializeDatasetToJson(originalDataset, _jsonDicomConverter);
            DicomDataset recoDataset = DicomTypeTranslater.DeserializeJsonToDataset(json, _jsonDicomConverter);
            string json2 = DicomTypeTranslater.SerializeDatasetToJson(recoDataset, _jsonDicomConverter);

            Assert.AreEqual(json, json2);

            // NOTE: Group length elements have been retired from the standard, and have never been included in any JSON conversion.
            // Remove them here to allow comparison between datasets.
            originalDataset.RemoveGroupLengths();

            Assert.True(TranslationTestHelpers.ValueEquals(originalDataset, recoDataset));
        }

        private static void ValidatePrivateCreatorsExist(DicomDataset dataset)
        {
            foreach (DicomItem item in dataset)
            {
                if ((item.Tag.Element & 0xff00) != 0)
                    Assert.False(string.IsNullOrWhiteSpace(item.Tag.PrivateCreator?.Creator));

                Assert.NotNull(item.Tag.DictionaryEntry);

                if (item.ValueRepresentation != DicomVR.SQ)
                    continue;

                foreach (DicomDataset subDs in ((DicomSequence)item).Items)
                    ValidatePrivateCreatorsExist(subDs);
            }
        }

        #endregion

        #region Tests

        [Test]
        public void TestFile_Image11()
        {
            File.WriteAllBytes(_testDcmPath, TestStructuredReports.image11);
            DicomDataset ds = DicomFile.Open(_testDcmPath).Dataset;
            ds.Remove(DicomTag.PixelData);

            string convType = _jsonDicomConverter.GetType().Name;
            switch (convType)
            {
                case "SmiLazyJsonDicomConverter":
                    VerifyJsonTripleTrip(ds);
                    break;

                case "SmiStrictJsonDicomConverter":
                case "JsonDicomConverter":
                    Assert.Throws<FormatException>(() => VerifyJsonTripleTrip(ds), $"[{convType}] Expected FormatException parsing 2.500000 as an IntegerString");
                    break;

                default:
                    Assert.Fail($"No test case for {convType}");
                    break;
            }
        }

        [Test]
        public void TestFile_Report01()
        {
            File.WriteAllBytes(_testDcmPath, TestStructuredReports.report01);
            DicomDataset ds = DicomFile.Open(_testDcmPath).Dataset;
            VerifyJsonTripleTrip(ds);
        }

        [Test]
        public void TestJsonConversionAllVrs()
        {
            VerifyJsonTripleTrip(TranslationTestHelpers.BuildVrDataset());
        }

        [Test]
        public void TestZooDataset()
        {
            DicomDataset ds = TranslationTestHelpers.BuildZooDataset();
            VerifyJsonTripleTrip(ds);
        }

        [Test]
        public void TestNullDataset()
        {
            DicomDataset ds = TranslationTestHelpers.BuildAllTypesNullDataset();
            VerifyJsonTripleTrip(ds);
        }

        [Test]
        public void TestBlankAndNullSerialization()
        {
            var dataset = new DicomDataset
            {
                new DicomDecimalString(DicomTag.SelectorDSValue, default(string)),
                new DicomIntegerString(DicomTag.SelectorISValue, ""),
                new DicomFloatingPointSingle(DicomTag.SelectorFLValue, default(float)),
                new DicomFloatingPointDouble(DicomTag.SelectorFDValue)
            };

            string json = DicomTypeTranslater.SerializeDatasetToJson(dataset, _jsonDicomConverter);
            _logger.Debug(json);

            DicomDataset recoDataset = DicomTypeTranslater.DeserializeJsonToDataset(json, _jsonDicomConverter);
            Assert.True(TranslationTestHelpers.ValueEquals(dataset, recoDataset));

            // Move Bson stuff
            BsonDocument doc = DicomTypeTranslaterReader.BuildDatasetDocument(recoDataset);
            _logger.Debug(doc.ToJson());

            recoDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(doc);
            Assert.True(TranslationTestHelpers.ValueEquals(dataset, recoDataset));
        }

        [Test]
        public void TestPrivateTagsDeserialization()
        {
            DicomPrivateCreator privateCreator = DicomDictionary.Default.GetPrivateCreator("Testing");
            var privateTag1 = new DicomTag(4013, 0x008, privateCreator);
            var privateTag2 = new DicomTag(4013, 0x009, privateCreator);

            var privateDs = new DicomDataset
            {
                { DicomTag.Modality, "CT" },
                new DicomCodeString(privateTag1, "test1"),
                { privateTag2, "test2" },
            };

            VerifyJsonTripleTrip(privateDs);
        }

        [Test]
        public void Deserialize_PrivateCreators_AreSet()
        {
            DicomDataset originalDataset = TranslationTestHelpers.BuildPrivateDataset();

            ValidatePrivateCreatorsExist(originalDataset);

            string json = JsonConvert.SerializeObject(originalDataset, new JsonDicomConverter());
            var recoDs = JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter());

            ValidatePrivateCreatorsExist(recoDs);
        }

        [Test]
        public void TestMaskedTagSerialization()
        {
            //Example: OverlayRows element has a masked tag of (60xx,0010)
            _logger.Info("DicomTag.OverlayRows.DictionaryEntry.MaskTag: " + DicomTag.OverlayRows.DictionaryEntry.MaskTag);

            string rawJson = "{\"60000010\":{\"vr\":\"US\",\"Value\":[128]},\"60000011\":{\"vr\":\"US\",\"Value\":[614]},\"60000040\":{\"vr\":\"CS\",\"Value\":[\"G\"]},\"60000050\":{\"vr\":\"SS\",\"Value\":[0,0]},\"60000100\":{\"vr\":\"US\",\"Value\":[1]},\"60000102\":{\"vr\":\"US\",\"Value\":[0]},\"60020010\":{\"vr\":\"US\",\"Value\":[512]},\"60020011\":{\"vr\":\"US\",\"Value\":[614]},\"60020040\":{\"vr\":\"CS\",\"Value\":[\"G\"]},\"60020050\":{\"vr\":\"SS\",\"Value\":[0,0]},\"60020100\":{\"vr\":\"US\",\"Value\":[1]},\"60020102\":{\"vr\":\"US\",\"Value\":[0]}}";

            DicomDataset maskDataset = DicomTypeTranslater.DeserializeJsonToDataset(rawJson, new SmiStrictJsonDicomConverter());

            foreach (DicomItem item in maskDataset.Where(x => x.Tag.DictionaryEntry.Keyword == DicomTag.OverlayRows.DictionaryEntry.Keyword))
                _logger.Debug("{0} {1} - Val: {2}", item.Tag, item.Tag.DictionaryEntry.Keyword, maskDataset.GetString(item.Tag));

            VerifyJsonTripleTrip(maskDataset);
        }


        [Test]
        public void TestDecimalStringSerialization()
        {
            string[] testValues = { ".123", ".0", "5\0", " 0000012.", "00", "00.0", "-.123" };
            string[] expectedValues = { "0.123", "0.0", "5", "12.0", "0", "0.0", "-0.123" };

            var ds = new DicomDataset
            {
                new DicomDecimalString(DicomTag.PatientWeight, "")
            };

            string json = DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter);

            Assert.True(json.Equals("{\"00101030\":{\"vr\":\"DS\"}}"));

            string convType = _jsonDicomConverter.GetType().Name;

            for (var i = 0; i < testValues.Length; i++)
            {
                ds.AddOrUpdate(DicomTag.PatientWeight, testValues[i]);

                switch (convType)
                {
                    case "SmiLazyJsonDicomConverter":
                        json = DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter);
                        string expected = testValues[i].TrimEnd('\0');
                        Assert.AreEqual($"{{\"00101030\":{{\"vr\":\"DS\",\"val\":\"{expected}\"}}}}", json);
                        break;

                    case "SmiStrictJsonDicomConverter":
                        json = DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter);
                        Assert.True(json.Equals("{\"00101030\":{\"vr\":\"DS\",\"Value\":[" + expectedValues[i] + "]}}"));
                        break;

                    case "JsonDicomConverter":
                        if (i == 2 || i == 3)
                            Assert.Throws<ArgumentException>(() => DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter));
                        else
                        {
                            json = DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter);
                            Assert.True(json.Equals("{\"00101030\":{\"vr\":\"DS\",\"Value\":[" + expectedValues[i] + "]}}"));
                        }
                        break;

                    default:
                        Assert.Fail($"No test case for {convType}");
                        break;
                }
            }

            if (_jsonDicomConverter.GetType().Name != "SmiLazyJsonDicomConverter")
                return;

            // Test all in a single element
            ds.AddOrUpdate(DicomTag.PatientWeight, testValues);
            VerifyJsonTripleTrip(ds);
        }


        [Test]
        public void TestLongIntegerStringSerialization()
        {

            var ds = new DicomDataset();

            string[] testValues =
            {
                "123",              // Normal value
                "-123",             // Normal value
                "00000000",         // Technically allowed
                "+123",             // Strange, but allowed
                "00001234123412"    // A value we can fix and parse
            };

            // Values which will be 'fixed' by the SmiStrictJsonDicomConverter
            string[] expectedValues = { "123", "-123", "0", "123", "1234123412" };

            string convType = _jsonDicomConverter.GetType().Name;

            for (var i = 0; i < testValues.Length; i++)
            {
                ds.AddOrUpdate(new DicomIntegerString(DicomTag.SelectorAttributePrivateCreator, testValues[i]));

                switch (convType)
                {
                    case "SmiLazyJsonDicomConverter":
                        string json = DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter);
                        Assert.AreEqual($"{{\"00720056\":{{\"vr\":\"IS\",\"val\":\"{testValues[i]}\"}}}}", json);
                        break;

                    case "SmiStrictJsonDicomConverter":
                    case "JsonDicomConverter":
                        json = DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter);
                        Assert.AreEqual(("{\"00720056\":{\"vr\":\"IS\",\"Value\":[" + expectedValues[i] + "]}}"), json);
                        break;

                    default:
                        Assert.Fail($"No test case for {convType}");
                        break;
                }
            }

            // Value larger than an Int32
            ds.AddOrUpdate(new DicomIntegerString(DicomTag.SelectorAttributePrivateCreator, "10001234123412"));

            switch (convType)
            {
                case "JsonDicomConverter":
                    // Default converter will try and force it into an int -> OverflowException
                    Assert.Throws<OverflowException>(() => DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter));
                    break;

                case "SmiStrictJsonDicomConverter":
                    // Strict converter will try and 'fix' it, before throwing a FormatException
                    Assert.Throws<FormatException>(() => DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter));
                    break;

                case "SmiLazyJsonDicomConverter":
                    // Our converter doesn't enforce types, so this should pass
                    string json = DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter);
                    Assert.AreEqual("{\"00720056\":{\"vr\":\"IS\",\"val\":\"10001234123412\"}}", json);
                    break;

                default:
                    Assert.Fail($"No test case for {convType}");
                    break;
            }
        }

        #endregion
    }
}
