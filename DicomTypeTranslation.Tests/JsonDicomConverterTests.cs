
using Dicom;
using Dicom.Serialization;
using DicomTypeTranslation.Converters;
using DicomTypeTranslation.Tests.Helpers;
using MongoDB.Bson;
using Newtonsoft.Json;
using NLog;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using DicomTypeTranslation.Helpers;

namespace DicomTypeTranslation.Tests
{
    [TestFixture(ConverterTestCase.Standard)]
    [TestFixture(ConverterTestCase.SmiStrict)]
    [TestFixture(ConverterTestCase.SmiLazy)]
    public class JsonDicomConverterTests
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly JsonConverter _jsonDicomConverter;


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

        #region Fixture Methods 
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {

        }

        #endregion

        #region Test Helpers

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
            Assert.True(DicomDatasetHelpers.ValueEquals(originalDataset, recoDataset));
        }

        #endregion

        #region Tests

        [Test]
        public void TestJsonConversionAllVrs()
        {
            VerifyJsonTripleTrip(TranslationTestHelpers.BuildVrDataset());
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
            Assert.True(DicomDatasetHelpers.ValueEquals(dataset, recoDataset));

            BsonDocument doc = DicomTypeTranslaterReader.BuildDatasetDocument(recoDataset);
            _logger.Debug(doc.ToJson());

            recoDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(doc);
            Assert.True(DicomDatasetHelpers.ValueEquals(dataset, recoDataset));
        }

        [Test]
        public void TestPrivateTagsDeserialization()
        {
            DicomPrivateCreator privateCreator = DicomDictionary.Default.GetPrivateCreator("Testing");
            var privateTag1 = new DicomTag(4013, 0x008, privateCreator);
            var privateTag2 = new DicomTag(4013, 0x009, privateCreator);

            var ds = new DicomDataset
            {
                { DicomTag.Modality, "CT" },
                new DicomCodeString(privateTag1, "test1"),
                { privateTag2, "test2" },
            };

            string json = DicomTypeTranslater.SerializeDatasetToJson(ds, _jsonDicomConverter);
            _logger.Debug(json);

            DicomDataset reco = DicomTypeTranslater.DeserializeJsonToDataset(json, _jsonDicomConverter);
            Assert.True(DicomDatasetHelpers.ValueEquals(ds, reco));
        }

        [Test]
        public void TestMaskedTagSerialization()
        {
            //Example: OverlayRows element has a masked tag of (60xx,0010)
            _logger.Info("DicomTag.OverlayRows.DictionaryEntry.MaskTag: " + DicomTag.OverlayRows.DictionaryEntry.MaskTag);

            const string rawJson = "{\"60000010\":{\"vr\":\"US\",\"Value\":[128]},\"60000011\":{\"vr\":\"US\",\"Value\":[614]},\"60000040\":{\"vr\":\"CS\",\"Value\":[\"G\"]},\"60000050\":{\"vr\":\"SS\",\"Value\":[0,0]},\"60000100\":{\"vr\":\"US\",\"Value\":[1]},\"60000102\":{\"vr\":\"US\",\"Value\":[0]},\"60020010\":{\"vr\":\"US\",\"Value\":[512]},\"60020011\":{\"vr\":\"US\",\"Value\":[614]},\"60020040\":{\"vr\":\"CS\",\"Value\":[\"G\"]},\"60020050\":{\"vr\":\"SS\",\"Value\":[0,0]},\"60020100\":{\"vr\":\"US\",\"Value\":[1]},\"60020102\":{\"vr\":\"US\",\"Value\":[0]}}";

            DicomDataset maskDataset = DicomTypeTranslater.DeserializeJsonToDataset(rawJson, new SmiStrictJsonDicomConverter());

            foreach (DicomItem item in maskDataset.Where(x => x.Tag.DictionaryEntry.Keyword == DicomTag.OverlayRows.DictionaryEntry.Keyword))
                _logger.Debug("{0} {1} - Val: {2}", item.Tag, item.Tag.DictionaryEntry.Keyword, maskDataset.GetString(item.Tag));

            VerifyJsonTripleTrip(maskDataset);
        }

        #endregion

        public enum ConverterTestCase
        {
            Standard,   // The standard Json converter used in fo-dicom. Conforms to Dicom specification for Json representation
            SmiStrict,  // Our version of the standard converter, with some extra handling of 'dodgy' data
            SmiLazy     // Our custom version of the converter which just uses the underlying element value types used by fo-dicom
        }
    }
}
