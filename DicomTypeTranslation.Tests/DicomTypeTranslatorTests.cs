using Dicom;
using DicomTypeTranslation.Tests.Helpers;
using MongoDB.Bson;
using Newtonsoft.Json;
using NLog;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DicomTypeTranslation.Helpers;

namespace DicomTypeTranslation.Tests
{
    [TestFixture]
    public class DicomTypeTranslatorTests
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {

        }

        #region Tests

        [Test]
        [Ignore("Ignore unless you want to test some specific files")]
        public void TestMultipleFiles()
        {
            // Add some local files to test
            DicomFile[] files =
            {
                DicomFile.Open(@"")
            };

            if (files.Length == 0)
                Assert.Fail("No files specified");

            foreach (var file in files)
                TestDatasetSerializationFromFile(file);
        }

        /// <summary>
        /// We can use the JsonDicomConverter fairly safely with the following caveats:
        /// We can't serialize PixelData/OB VRs for now
        /// We have to replace any null terminators with spaces (\0 doesn't conform to the standard but can be in the tags anyway!)
        /// We can't compare some sequences (only seen for private tags so far so might not be an issue) 
        /// </summary>
        /// <param name="dFile"></param>
        private static void TestDatasetSerializationFromFile(DicomFile dFile)
        {
            // Arrange

            // "Fix" for DS and IS values in original dataset so we can compare them properly after
            for (var i = 0; i < dFile.Dataset.Count(); i++)
            {
                var item = dFile.Dataset.ElementAt(i);

                if (item.ValueRepresentation == DicomVR.DS ||
                    item.ValueRepresentation == DicomVR.IS)
                {
                    // Serializer changes any null terminators to spaces
                    var value = dFile.Dataset.GetValue<string>(item.Tag, 0).Replace("\0", " ");

                    // Serializer changes "-0" to "0"
                    if (value.Equals("-0"))
                        value = "0";

                    dFile.Dataset.AddOrUpdate(item.Tag, value);
                }
            }

            // Can't serialize PixelData for now (index out of range stuff)
            dFile.Dataset.Remove(DicomTag.PixelData);

            // Act

            var json = DicomTypeTranslater.SerializeDatasetToJson(dFile.Dataset);
            var outDataset = DicomTypeTranslater.DeserializeJsonToDataset(json);
            var json2 = DicomTypeTranslater.SerializeDatasetToJson(outDataset);

            // Assert

            Assert.True(json.Equals(json2));

            if (outDataset.Count() != dFile.Dataset.Count())
                Assert.Fail("Datasets not the same size");

            for (var i = 0; i < outDataset.Count(); i++)
            {
                var origItem = dFile.Dataset.ElementAt(i);
                var outItem = outDataset.ElementAt(i);

                // This only compares DicomTag values
                if (outItem.CompareTo(origItem) != 0)
                    Assert.Fail("DicomTag values do not match");

                if (DicomDatasetHelpers.ValueEquals(origItem, outItem))
                    continue;

                if (origItem.Tag.IsPrivate)
                {
                    Console.WriteLine("Skipping equality check fro private tag: " + origItem);
                    continue;
                }

                if (origItem is DicomFragmentSequence)
                {
                    Console.WriteLine("Could not test equality of: " + origItem);
                    continue;
                }

                if (Enumerable.SequenceEqual(((DicomElement)origItem).Buffer.Data,
                    ((DicomElement)outItem).Buffer.Data))
                    continue;

                Console.WriteLine("Not equal: " +
                                  Encoding.UTF8.GetString(((DicomElement)origItem).Buffer.Data) + " || " +
                                  Encoding.UTF8.GetString(((DicomElement)outItem).Buffer.Data));

                Assert.Fail("Data buffers not equal");
            }
        }

        /// <summary>
        /// Basic test to see if the method crashes on any VRs
        /// </summary>
        [Test]
        public void TestBasicCSharpTranslation()
        {
            var ds = TranslationTestHelpers.BuildVrDataset();

            foreach (var item in ds)
            {
                var val = DicomTypeTranslaterReader.GetCSharpValue(ds, item);

                //TODO Looks like we have some issues with encoding?
                Console.WriteLine(val);
            }
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
            Assert.True(asArray != null);

            Assert.True(asArray.Length == 1);
            Assert.True(asArray[0].Count == 2);

            Assert.True(asArray[0][DicomTag.SpecimenShortDescription].Equals("short desc"));
            Assert.True(asArray[0][DicomTag.PatientAge].Equals("99Y"));
        }

        [Test]
        public void TestWriteMultiplicity()
        {
            DicomTag stringMultiTag = DicomTag.SpecimenShortDescription;
            string[] vals = { "this", "is", "a", "multi", "element", "" };

            var ds = new DicomDataset();

            DicomTypeTranslaterWriter.SetDicomTag(ds, stringMultiTag, vals);

            Assert.True(ds.Count() == 1);
            Assert.True(ds.GetDicomItem<DicomElement>(stringMultiTag).Count == 6);
            Assert.True(ds.GetString(stringMultiTag).Equals("this\\is\\a\\multi\\element\\"));
        }

        [Test]
        public void TestDecimalStringSerialization()
        {
            string[] testValues = { ".123", ".0", "5\0", " 0000012.", "00", "00.0", "-.123" };
            string[] expectedValues = { "0.123", "0.0", "5", "12.0", "0", "0.0", "-0.123" };

            var dataset = new DicomDataset
            {
                new DicomDecimalString(DicomTag.PatientWeight, "")
            };

            var json = DicomTypeTranslater.SerializeDatasetToJson(dataset);
            Console.WriteLine(json);
            Assert.NotNull(json);
            Assert.True(json.Equals("{\"00101030\":{\"vr\":\"DS\"}}"));

            for (var i = 0; i < testValues.Length; i++)
            {
                dataset.AddOrUpdate(DicomTag.PatientWeight, testValues[i]);
                json = DicomTypeTranslater.SerializeDatasetToJson(dataset);

                Console.WriteLine(json);
                Assert.NotNull(json);
                Assert.True(json.Equals("{\"00101030\":{\"vr\":\"DS\",\"Value\":[" + expectedValues[i] + "]}}"));
            }

            dataset.AddOrUpdate(DicomTag.PatientWeight, "      ");
            json = DicomTypeTranslater.SerializeDatasetToJson(dataset);
            Console.WriteLine(json);
            Assert.NotNull(json);
            Assert.True(json.Equals("{\"00101030\":{\"vr\":\"DS\"}}"));

            DicomDataset recoDataset = DicomTypeTranslater.DeserializeJsonToDataset(json);
            Assert.NotNull(recoDataset);

        }

        [Test]
        public void TestLongIntegerStringSerialization()
        {
            var dataset = new DicomDataset();

            string[] testValues =
            {
                "123",              // Normal value
                "-123",             // Normal value
                "00000000",         // Technically allowed
                "+123",             // Strange, but allowed
                "00001234123412"    // A value we can fix and parse
            };

            string[] expectedValues = { "123", "-123", "0", "123", "1234123412" };

            for (var i = 0; i < testValues.Length; i++)
            {
                dataset.AddOrUpdate(new DicomIntegerString(DicomTag.SelectorAttributePrivateCreator, testValues[i]));

                var json = DicomTypeTranslater.SerializeDatasetToJson(dataset);

                Assert.IsFalse(string.IsNullOrWhiteSpace(json));
                Assert.True(json.Equals("{\"00720056\":{\"vr\":\"IS\",\"Value\":[" + expectedValues[i] + "]}}"));
            }

            // Value we can't fix and parse
            dataset.AddOrUpdate(new DicomIntegerString(DicomTag.SelectorAttributePrivateCreator, "10001234123412"));

            Assert.Throws<FormatException>(() => DicomTypeTranslater.SerializeDatasetToJson(dataset));
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

            var originaldataset = new DicomDataset
            {
                {DicomTag.ReferencedImageSequence, subDatasets.ToArray()}
            };


            var translatedDataset = new Dictionary<DicomTag, object>();

            foreach (var item in originaldataset)
            {
                try
                {
                    object value = DicomTypeTranslaterReader.GetCSharpValue(originaldataset, item);
                    translatedDataset.Add(item.Tag, value);
                }
                catch (Exception e)
                {
                    Assert.Fail(e.Message);
                }
            }

            var reconstructedDataset = new DicomDataset();

            foreach (KeyValuePair<DicomTag, object> item in translatedDataset)
                DicomTypeTranslaterWriter.SetDicomTag(reconstructedDataset, item.Key, item.Value);

            Assert.True(DicomDatasetHelpers.ValueEquals(originaldataset, reconstructedDataset));
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

            Console.WriteLine(TranslationTestHelpers.PrettyPrintBsonDocument(usDocument));
            Console.WriteLine(TranslationTestHelpers.PrettyPrintBsonDocument(owDocument));

            DicomDataset recoUsDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(usDocument);
            DicomDataset recoOwDataset = DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument(owDocument);

            Assert.True(DicomDatasetHelpers.ValueEquals(usDataset, recoUsDataset));

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

            Assert.True(DicomDatasetHelpers.ValueEquals(recoDsFromPrefix, recoDsFromNoPrefix));
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

        [TestCase(typeof(Dicom.Serialization.JsonDicomConverter), PrivateTagTestCase.SingleStringTag)]
        [TestCase(typeof(Dicom.Serialization.JsonDicomConverter), PrivateTagTestCase.SingleCodeStringTag)]
        [TestCase(typeof(Dicom.Serialization.JsonDicomConverter), PrivateTagTestCase.TwoStrings)]
        public void TestPrivateTagsDeserialization_String(Type converterType, PrivateTagTestCase testCase)
        {
            DicomPrivateCreator privateCreator = DicomDictionary.Default.GetPrivateCreator("TestPrivateCreator");

            var privTag1 = new DicomTag(4013, 0x007, privateCreator);
            var privTag2 = new DicomTag(4013, 0x009, privateCreator);

            DicomDataset ds;

            switch (testCase)
            {
                case PrivateTagTestCase.SingleStringTag:
                    ds = new DicomDataset
                        {
                            {privTag1, "test1"}
                        };
                    break;
                case PrivateTagTestCase.SingleCodeStringTag:
                    ds = new DicomDataset
                        {
                            new DicomCodeString(privTag1, "test1"),
                        };
                    break;
                case PrivateTagTestCase.TwoStrings:
                    ds = new DicomDataset
                        {
                            {privTag2, "test2"},
                            {privTag1, "test1"}
                        };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("testCase");
            }


            BindingFlags flags = BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance | BindingFlags.OptionalParamBinding;
            object converter = Activator.CreateInstance(converterType, flags, null, new[] { Type.Missing }, null);

            string json = DicomTypeTranslater.SerializeDatasetToJson(ds, (JsonConverter)converter);
            DicomDataset ds2 = DicomTypeTranslater.DeserializeJsonToDataset(json, (JsonConverter)converter);

            Console.WriteLine("Dataset 1:");
            foreach (DicomItem item in ds)
                Console.WriteLine("{0}:{1}", item.Tag, ds.GetValue<string>(item.Tag, 0));

            Console.WriteLine("Dataset 2:");
            foreach (DicomItem item in ds2)
                Console.WriteLine("{0}:{1}", item.Tag, ds2.GetValue<string>(item.Tag, 0));

            Assert.AreEqual(ds.GetValue<string>(privTag1, 0), ds2.GetValue<string>(privTag1, 0));
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

            StringAssert.Contains("CPELVD",(string)flat);
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
        //TODO Move to jsonconverter test classes
        public void TestVrCoverage()
        {
            PropertyInfo[] vrProps = typeof(DicomVR).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var sb = new StringBuilder();

            foreach (DicomVR vr in TranslationTestHelpers.AllVrCodes)
            {
                sb.Clear();

                foreach (PropertyInfo prop in vrProps)
                    sb.Append(prop.Name + ": " + prop.GetValue(vr) + ", ");

                sb.Length -= 2;
                _logger.Info(sb);
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
