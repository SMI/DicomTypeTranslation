
using Dicom;
using DicomTypeTranslation.Tests.Helpers;
using MongoDB.Bson;
using NLog;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DicomTypeTranslation.Tests
{
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

        #endregion

        #region Tests

        [Test]
        public void TestBasicCSharpTranslation()
        {
            DicomDataset ds = TranslationTestHelpers.BuildVrDataset();

            foreach (DicomItem item in ds)
                Assert.NotNull(DicomTypeTranslaterReader.GetCSharpValue(ds, item));
        }

        [Test]
        public void TestSequenceConversion()
        {
            var subDataset = new DicomDataset
            {
                new DicomShortString(DicomTag.SpecimenShortDescription, "short desc"),
                new DicomAgeString(DicomTag.PatientAge, "99Y")
            };

            var ds = new DicomDataset
            {
                new DicomSequence(DicomTag.ReferencedImageSequence, subDataset)
            };

            object obj = DicomTypeTranslaterReader.GetCSharpValue(ds, ds.GetDicomItem<DicomItem>(DicomTag.ReferencedImageSequence));

            var asArray = obj as Dictionary<DicomTag, object>[];
            Assert.NotNull(asArray);

            Assert.AreEqual(1, asArray.Length);
            Assert.AreEqual(2, asArray[0].Count);

            Assert.AreEqual("short desc", asArray[0][DicomTag.SpecimenShortDescription]);
            Assert.AreEqual("99Y", asArray[0][DicomTag.PatientAge]);
        }

        [Test]
        public void TestWriteMultiplicity()
        {
            DicomTag stringMultiTag = DicomTag.SpecimenShortDescription;
            string[] values = { "this", "is", "a", "multi", "element", "" };

            var ds = new DicomDataset();

            DicomTypeTranslaterWriter.SetDicomTag(ds, stringMultiTag, values);

            Assert.AreEqual(1, ds.Count());
            Assert.AreEqual(6, ds.GetDicomItem<DicomElement>(stringMultiTag).Count);
            Assert.AreEqual("this\\is\\a\\multi\\element\\", ds.GetString(stringMultiTag));
        }

        [Test]
        public void TestMultipleElementSequences()
        {
            var subDatasets = new List<DicomDataset>();
            for (var i = 0; i < 3; i++)
            {
                subDatasets.Add(new DicomDataset
                {
                    {DicomTag.ReferencedSOPClassUID, "ReferencedSOPClassUID-" + (i + 1)},
                    {DicomTag.ReferencedSOPInstanceUID, "ReferencedSOPInstanceUID-" + (i + 1)}
                });
            }

            var originalDataset = new DicomDataset
            {
                {DicomTag.ReferencedImageSequence, subDatasets.ToArray()}
            };

            var translatedDataset = new Dictionary<DicomTag, object>();

            foreach (DicomItem item in originalDataset)
            {
                object value = DicomTypeTranslaterReader.GetCSharpValue(originalDataset, item);
                translatedDataset.Add(item.Tag, value);
            }

            var reconstructedDataset = new DicomDataset();

            foreach (KeyValuePair<DicomTag, object> item in translatedDataset)
                DicomTypeTranslaterWriter.SetDicomTag(reconstructedDataset, item.Key, item.Value);

            Assert.True(TranslationTestHelpers.ValueEquals(originalDataset, reconstructedDataset));
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
            Assert.True(TranslationTestHelpers.ValueEquals(dataset, reconstructedDataset));
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
                Assert.True(TranslationTestHelpers.ValueEquals(dataset, reconstructedDataset));
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

            Console.WriteLine(TranslationTestHelpers.PrettyPrintBsonDocument(usDocument));
            Console.WriteLine(TranslationTestHelpers.PrettyPrintBsonDocument(owDocument));

            DicomDataset recoUsDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(usDocument);
            DicomDataset recoOwDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(owDocument);

            Assert.True(TranslationTestHelpers.ValueEquals(usDataset, recoUsDataset));

            //TODO This will fail. Although the tag and values are identical, the original is a DicomOtherWord element and the reconstructed one is a DicomUnsignedShort
            //Assert.True(ValueEquals(owDataset, recoOwDataset));
        }

        [Test]
        public void TestBothBsonTagFormats()
        {
            // We will store tags in MongoDB in either of the following formats, and have to handle both cases:
            // (0123,4567)-TagDictionaryName
            // TagDictionaryName

            var ds = new DicomDataset
            {
                new DicomShortString(DicomTag.SelectorSHValue, "ShortStringValue")
            };

            BsonDocument bsonWithPrefix = DicomTypeTranslaterReader.BuildDatasetDocument(ds, true);
            BsonDocument bsonWithoutPrefix = DicomTypeTranslaterReader.BuildDatasetDocument(ds, false);

            DicomDataset recoDsFromPrefix = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(bsonWithPrefix);
            DicomDataset recoDsFromNoPrefix = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(bsonWithoutPrefix);

            Assert.True(TranslationTestHelpers.ValueEquals(recoDsFromPrefix, recoDsFromNoPrefix));
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

            Assert.True(dataset.Count() == 2);

            var asElement = dataset.GetDicomItem<DicomElement>(DicomTag.SelectorASValue);
            Assert.True(asElement.Buffer.Size == 0);

            var flElement = dataset.GetDicomItem<DicomElement>(DicomTag.SelectorFLValue);
            Assert.True(flElement.Buffer.Size == 0);
        }

        [Test]
        public void Test_Sequence()
        {
            var subDatasets = new List<DicomDataset>();

            subDatasets.Add(new DicomDataset
            {
                new DicomShortString(DicomTag.CodeValue,"CPELVD")
            });


            var originaldataset = new DicomDataset
            {
                {DicomTag.ProcedureCodeSequence, subDatasets.ToArray()}
            };

            var result = DicomTypeTranslaterReader.GetCSharpValue(originaldataset, DicomTag.ProcedureCodeSequence);


            var flat = DicomTypeTranslater.Flatten(result);
            Console.WriteLine(flat);

            StringAssert.Contains("CPELVD", (string)flat);
            StringAssert.Contains("(0008,0100)", (string)flat);
        }

        [Test]
        public void TestBsonDatasetRebuild_WithPrivateTags_CreatorExists()
        {
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
            Assert.True(TranslationTestHelpers.ValueEquals(dataset, recoDataset));
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

        [Test]
        public void TestPatientAgeTag()
        {
            var dataset = new DicomDataset();

            dataset.Add(new DicomAgeString(DicomTag.PatientAge, "009Y"));

            var cSharpValue = DicomTypeTranslaterReader.GetCSharpValue(dataset, DicomTag.PatientAge);

            Assert.AreEqual("009Y", cSharpValue);
        }

        [Test]
        public void TestMaxVrMultiplicity()
        {
            foreach (DicomDictionaryEntry entry in DicomDictionary.Default)
            {
                if (entry.ValueRepresentations.Length <= DicomTypeTranslaterWriter.MaxVrsToExpect)
                    continue;

                Console.WriteLine(entry.Name + " has " + entry.ValueRepresentations.Length +
                                  " representations (" + string.Join(", ", entry.ValueRepresentations.Select(x => x.Name)) + ")");

                Assert.Fail("Element has more value representations than we would expect");
            }
        }

        [Test]
        public void PrintValueTypesForVrs()
        {
            DicomVR[] vrs = TranslationTestHelpers.AllVrCodes;
            var uniqueTypes = new SortedSet<string>();

            foreach (DicomVR vr in vrs)
            {
                //SQ value representation doesn't have ValueType defined
                if (vr == DicomVR.SQ)
                    continue;

                _logger.Info("VR: " + vr.Code + "\t Type: " + vr.ValueType.Name + "\t IsString: " + vr.IsString);
                uniqueTypes.Add(vr.ValueType.Name.TrimEnd(']', '['));
            }

            var sb = new StringBuilder();
            foreach (string str in uniqueTypes)
                sb.Append(str + ", ");

            sb.Length -= 2;
            _logger.Info("Unique underlying types: " + sb);
        }

        [Test]
        public void TestGetCSharpValueThrowsException()
        {
            var ds = new DicomDataset
            {
                new DicomDecimalString(DicomTag.SelectorDSValue, "aaahhhhh")
            };

            Assert.Throws<FormatException>(() => DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.SelectorDSValue));
        }

        #endregion
    }

    public enum PrivateTagTestCase
    {
        SingleStringTag,
        SingleCodeStringTag,
        TwoStrings
    }
}
