
using Dicom;
using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.Helpers;
using MongoDB.Bson;
using NLog;
using NUnit.Framework;
using System;
using System.Linq;

namespace DicomTypeTranslation.Tests
{
    [TestFixture]
    public class BsonTranslationTests
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        #endregion

        #region Tests

        [Test]
        public void TestStub()
        {
            //TODO: Add tests to demonstrate expectations from straight Bson->DicomDataset and DicomDataset->Bson conversions
            Assert.Fail();
        }


        [Test]
        public void TestDicomBsonMappingRoundTrip_Simple()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, "SOPInstanceUID-Test" }
            };

            BsonDocument document = DicomTypeTranslaterReader.BuildDatasetDocument(dataset);

            Assert.NotNull(document);

            DicomDataset reconstructedDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(document);

            Assert.NotNull(reconstructedDataset);
            Assert.True(DicomDatasetHelpers.ValueEquals(dataset, reconstructedDataset));
        }

        [Test]
        public void TestConvertingEmptyDicomElements()
        {
            var dataset = new DicomDataset
            {
                new DicomFloatingPointSingle(DicomTag.SelectorFLValue),
                new DicomApplicationEntity(DicomTag.SelectorAEValue)
            };

            BsonDocument document = DicomTypeTranslaterReader.BuildDatasetDocument(dataset);

            Assert.True(document.Count() == 2);
            Assert.True(document[0] == BsonNull.Value);
            Assert.True(document[1] == BsonNull.Value);
        }

        [Test]
        public void TestDicomBsonMappingRoundTrip_AllTypes()
        {
            // Change this to select a specific VR
            DicomVR vrToTest = null;

            foreach (DicomVR vr in TranslationTestHelpers.AllVrCodes)
            {
                if (vrToTest != null && vr != vrToTest)
                    continue;

                DicomDataset dataset = TranslationTestHelpers.BuildVrDataset(vr);

                BsonDocument document = DicomTypeTranslaterReader.BuildDatasetDocument(dataset);
                Assert.NotNull(document);

                DicomDataset reconstructedDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(document);
                Assert.NotNull(reconstructedDataset);

                Assert.True(reconstructedDataset.Count() == 1);
                Assert.True(DicomDatasetHelpers.ValueEquals(dataset, reconstructedDataset));
            }
        }

        [Test]
        public void TestBsonDocumentWithAllVRs()
        {

            BsonDocument document = DicomTypeTranslaterReader.BuildDatasetDocument(TranslationTestHelpers.BuildVrDataset());
            Assert.NotNull(document);

            Console.WriteLine("=== Bson Dataset ===\n" + TranslationTestHelpers.PrettyPrintBsonDocument(document) + "\n===\t===");
        }

        [Test]
        public void TestTagWithMultipleVrs()
        {
            var usItem = new DicomUnsignedShort(DicomTag.GrayLookupTableDataRETIRED, 0, 1, ushort.MaxValue);
            var owItem = new DicomOtherWord(DicomTag.GrayLookupTableDataRETIRED, 0, 1, ushort.MaxValue);

            Assert.True(usItem.Tag.DictionaryEntry.ValueRepresentations.Length == 3);
            Assert.True(owItem.Tag.DictionaryEntry.ValueRepresentations.Length == 3);

            var usDataset = new DicomDataset { usItem };
            var owDataset = new DicomDataset { owItem };

            BsonDocument usDocument = DicomTypeTranslaterReader.BuildDatasetDocument(usDataset);
            BsonDocument owDocument = DicomTypeTranslaterReader.BuildDatasetDocument(owDataset);

            Assert.NotNull(usDocument);
            Assert.NotNull(owDocument);

            DicomDataset recoUsDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(usDocument);
            DicomDataset recoOwDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(owDocument);

            Assert.True(DicomDatasetHelpers.ValueEquals(usDataset, recoUsDataset));

            //TODO This will fail. Although the tag and values are identical, the original is a DicomOtherWord element and the reconstructed one is a DicomUnsignedShort
            //Note: Should be fixed by updates to the MongoDB (BSON) stuff
            Assert.True(DicomDatasetHelpers.ValueEquals(owDataset, recoOwDataset));
        }

        /// <summary>
        /// We will store tags in MongoDB in either of the following formats, and have to handle both cases:
        /// TagDictionaryName
        /// (0123,4567)-TagDictionaryName
        /// </summary>
        [Test]
        public void TestBsonKeyConversion()
        {
            DicomPrivateCreator privateCreator = DicomDictionary.Default.GetPrivateCreator("TEST");
            DicomDictionary pDict = DicomDictionary.Default[privateCreator];

            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx02"), "Private Tag 02", "PrivateTag02", DicomVM.VM_1, false, DicomVR.AE));

            var ds = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, "1.2.3.4" }
            };

            ds.Add(new DicomApplicationEntity(ds.GetPrivateTag(new DicomTag(3, 0x0002, privateCreator)), "AETITLE"));

            BsonDocument bsonDoc = DicomTypeTranslaterReader.BuildDatasetDocument(ds);

            //NOTE: Ordering of items inside a MongoDB document is significant
            var expected = new BsonDocument
            {
                { "(0003,0010)-PrivateCreator","TEST" },
                { "(0003,1002:TEST)-PrivateTag02", "AETITLE" },
                { "SOPInstanceUID", "1.2.3.4" }
            };

            Assert.AreEqual(expected, bsonDoc);
        }

        [Test]
        public void TestBsonDatasetRebuild_WithPrivateTags_CreatorExists()
        {
            //TODO Replace dataset with test one

            //Assert.Fail();
            // Convert a dataset with private tag information (existing in the default dictionary) to bson
            // Reconstruct and assert equality

            var dataset = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, "1.2.3.4" }
            };

            dataset.Add(new DicomCodeString(new DicomTag(1953, 16), "ELSCINT1"));
            dataset.Add(new DicomUnsignedShort(new DicomTag(1953, 4176, "ELSCINT1"), 20));

            BsonDocument bson = DicomTypeTranslaterReader.BuildDatasetDocument(dataset);
            Assert.NotNull(bson);

            DicomDataset recoDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(bson);

            Assert.NotNull(recoDataset);
            Assert.True(DicomDatasetHelpers.ValueEquals(dataset, recoDataset));
        }

        [Test]
        public void TestBsonDatasetRebuild_WithPrivateTags_CreatorMissing()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, "1.2.3.4" }
            };

            dataset.Add(new DicomCodeString(new DicomTag(1953, 16), "AAAHHHH"));
            dataset.Add(new DicomUnsignedShort(new DicomTag(1953, 4176, "AAAHHHH"), 20));

            BsonDocument bson = DicomTypeTranslaterReader.BuildDatasetDocument(dataset);
            Assert.NotNull(bson);

            Assert.Throws<ApplicationException>(() => DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(bson));
        }

        #endregion
    }
}
