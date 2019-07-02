
using System.IO;
using System.Linq;
using System.Text;

using Dicom;

using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.Helpers;

using MongoDB.Bson;

using NLog;

using NUnit.Framework;


namespace DicomTypeTranslation.Tests
{
    [TestFixture]
    public class DicomToBsonTranslationTests
    {
        #region Tests

        [Test]
        public void DicomToBson_TranslateEmptyElements_TagsRetainedValuesNull()
        {
            var dataset = new DicomDataset
            {
                new DicomFloatingPointSingle(DicomTag.SelectorFLValue),
                new DicomApplicationEntity(DicomTag.SelectorAEValue)
            };

            BsonDocument document = DicomTypeTranslaterReader.BuildBsonDocument(dataset);

            Assert.True(document.Count() == 2);
            Assert.True(document[0] == BsonNull.Value);
            Assert.True(document[1] == BsonNull.Value);
        }

        [Test]
        public void DicomToBson_TranslateNormalDataset_DoesNotThrow()
        {
            DicomDataset ds = TranslationTestHelpers.BuildVrDataset();
            Assert.DoesNotThrow(() => DicomTypeTranslaterReader.BuildBsonDocument(ds));
        }

        [Test]
        public void DicomToBson_TranslatePrivateDataset_DoesNotThrow()
        {
            DicomDataset ds = TranslationTestHelpers.BuildPrivateDataset();
            Assert.DoesNotThrow(() => DicomTypeTranslaterReader.BuildBsonDocument(ds));
        }

        /// <summary>
        /// We will store tags in MongoDB in either of the following formats, and have to handle both cases:
        /// TagDictionaryName
        /// (0123,4567)-TagDictionaryName
        /// </summary>
        [Test]
        public void DicomToBson_BothBsonKeyFormats_ConvertedCorrectly()
        {
            DicomPrivateCreator privateCreator = DicomDictionary.Default.GetPrivateCreator("TEST");
            DicomDictionary pDict = DicomDictionary.Default[privateCreator];

            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx02"), "Private Tag 02", "PrivateTag02", DicomVM.VM_1, false, DicomVR.AE));

            var ds = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, "1.2.3.4" }
            };

            ds.Add(new DicomApplicationEntity(ds.GetPrivateTag(new DicomTag(3, 0x0002, privateCreator)), "AETITLE"));

            BsonDocument bsonDoc = DicomTypeTranslaterReader.BuildBsonDocument(ds);

            //NOTE: Ordering of items inside a MongoDB document is significant
            var expected = new BsonDocument
            {
                { "(0003,0010)-PrivateCreator",
                    new BsonDocument
                    {
                        { "vr", "LO" },
                        { "val", "TEST" }
                    }
                },
                { "(0003,1002:TEST)-PrivateTag02",
                    new BsonDocument
                    {
                        { "vr", "AE" },
                        { "val", "AETITLE"}
                    }
                },
                { "SOPInstanceUID", "1.2.3.4" }
            };

            Assert.AreEqual(expected, bsonDoc);
        }

        /// <summary>
        /// Tests that VR information is correctly written to Bson when needed
        /// </summary>
        [Test]
        public void DicomToBson_VrInfo_StoredInBson()
        {
            // Dataset with:
            // Standard SOPInstanceUID tag 
            // Private tags
            // Tag with multiple possible VRs
            // Sequence containing private tags
            var ds = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, "1.2.3.4" },
                { DicomTag.SequenceOfUltrasoundRegions,
                    new DicomDataset
                    {
                        new DicomCodeString(new DicomTag(1953, 16), "ELSCINT1"),
                        new DicomUnsignedShort(new DicomTag(1953, 4176, "ELSCINT1"), 123),
                    },
                    new DicomDataset
                    {
                        new DicomCodeString(new DicomTag(1953, 16), "ELSCINT2"),
                        new DicomUnsignedShort(new DicomTag(1953, 4176, "ELSCINT2"), 456),
                    }
                },
                { DicomVR.US, DicomTag.GrayLookupTableDataRETIRED, ushort.MinValue, ushort.MaxValue },
                new DicomCodeString(new DicomTag(1953, 16), "ELSCINT3"),
                new DicomUnsignedShort(new DicomTag(1953, 4176, "ELSCINT3"), 789)
            };

            BsonDocument convertedDoc = DicomTypeTranslaterReader.BuildBsonDocument(ds);

            var expectedDoc = new BsonDocument
            {
                { "SOPInstanceUID", "1.2.3.4" },
                { "SequenceOfUltrasoundRegions",
                    new BsonArray
                    {
                        new BsonDocument
                        {
                            { "(07a1,0010)-PrivateCreator",
                                new BsonDocument
                                {
                                    { "vr", "CS" },
                                    { "val", "ELSCINT1" }
                                }
                            },
                            { "(07a1,1050:ELSCINT1)-Unknown",
                                new BsonDocument
                                {
                                    { "vr", "US" },
                                    { "val",
                                        new BsonArray
                                        {
                                            new BsonInt32(123)
                                        }
                                    }
                                }
                            }
                        },
                        new BsonDocument
                        {
                            { "(07a1,0010)-PrivateCreator",
                                new BsonDocument
                                {
                                    { "vr", "CS" },
                                    { "val", "ELSCINT2" }
                                }
                            },
                            { "(07a1,1050:ELSCINT2)-Unknown",
                                new BsonDocument
                                {
                                    { "vr", "US" },
                                    { "val",
                                        new BsonArray
                                        {
                                            new BsonInt32(456)
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                { "GrayLookupTableData",
                    new BsonDocument
                    {
                        { "vr", "US" },
                        { "val",
                            new BsonArray
                            {
                                ushort.MinValue,
                                ushort.MaxValue
                            }
                        }
                    }
                },
                { "(07a1,0010)-PrivateCreator",
                    new BsonDocument
                    {
                        { "vr", "CS" },
                        { "val", "ELSCINT3" }
                    }
                },
                { "(07a1,1050:ELSCINT3)-Unknown",
                    new BsonDocument
                    {
                        { "vr", "US" },
                        { "val",
                            new BsonArray
                            {
                                new BsonInt32(789)
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(expectedDoc, convertedDoc);
        }

        /// <summary>
        /// Asserts that group length elements (xxxx,0000) are ignored from BSON serialization
        /// </summary>
        [Test]
        public void DicomToBson_GroupLengthElements_AreIgnored()
        {
            var ds = new DicomDataset
            {
                new DicomCodeString(new DicomTag(123,0), "abc")
            };

            Assert.IsEmpty(DicomTypeTranslaterReader.BuildBsonDocument(ds));
        }

        [Test]
        public void DicomToBson_DicomAttributeTags_ConvertedCorrectly()
        {
            var ds = new DicomDataset
            {
                new DicomAttributeTag(DicomTag.SelectorATValue, DicomTag.SOPInstanceUID, DicomTag.SeriesInstanceUID)
            };

            var expected = new BsonDocument
            {
                { "SelectorATValue", "00080018\\0020000E" }
            };

            BsonDocument actual = DicomTypeTranslaterReader.BuildBsonDocument(ds);
            Assert.AreEqual(expected, actual);
        }

        #endregion
    }

    [TestFixture]
    public class BsonRoundTripTranslationTests
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        #endregion

        #region Test Helpers

        private static void VerifyBsonTripleTrip_BlacklistedRemoved(DicomDataset ds)
        {
            ds.Remove(x => DicomTypeTranslater.DicomBsonVrBlacklist.Contains(x.ValueRepresentation));

            BsonDocument bsonDoc = DicomTypeTranslaterReader.BuildBsonDocument(ds);
            DicomDataset recoDataset = DicomTypeTranslaterWriter.BuildDicomDataset(bsonDoc);
            BsonDocument recoDoc = DicomTypeTranslaterReader.BuildBsonDocument(recoDataset);

            Assert.AreEqual(bsonDoc, recoDoc);
            Assert.True(DicomDatasetHelpers.ValueEquals(ds, recoDataset));
        }

        #endregion

        #region Tests

        [Ignore("Ignore unless you want to test specific files")]
        //[TestCase("")]
        public void BsonRoundTrip_TestFile_PassesConversion(string filePath)
        {
            Assert.True(File.Exists(filePath));
            DicomDataset ds = DicomFile.Open(filePath).Dataset;
            VerifyBsonTripleTrip_BlacklistedRemoved(ds);
        }

        [Test]
        public void BsonRoundTrip_SimpleTag_SameAfterConversion()
        {
            var ds = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, "SOPInstanceUID-Test" }
            };

            VerifyBsonTripleTrip_BlacklistedRemoved(ds);
        }

        [Test]
        public void BsonRoundTrip_TagWithMultipleVrs_SameAfterConversion()
        {
            var usItem = new DicomUnsignedShort(DicomTag.GrayLookupTableDataRETIRED, 0, 1, ushort.MaxValue);
            var owItem = new DicomOtherWord(DicomTag.GrayLookupTableDataRETIRED, 0, 1, ushort.MaxValue);

            Assert.True(usItem.Tag.DictionaryEntry.ValueRepresentations.Length == 3);
            Assert.True(owItem.Tag.DictionaryEntry.ValueRepresentations.Length == 3);

            var usDataset = new DicomDataset { usItem };
            var owDataset = new DicomDataset { owItem };

            BsonDocument usDocument = DicomTypeTranslaterReader.BuildBsonDocument(usDataset);
            BsonDocument owDocument = DicomTypeTranslaterReader.BuildBsonDocument(owDataset);

            Assert.NotNull(usDocument);
            Assert.NotNull(owDocument);

            DicomDataset recoUsDataset = DicomTypeTranslaterWriter.BuildDicomDataset(usDocument);
            DicomDataset recoOwDataset = DicomTypeTranslaterWriter.BuildDicomDataset(owDocument);

            Assert.True(DicomDatasetHelpers.ValueEquals(usDataset, recoUsDataset));
            Assert.True(DicomDatasetHelpers.ValueEquals(owDataset, recoOwDataset));
        }

        [Test]
        public void BsonRoundTrip_PrivateDataset_SameAfterConversion()
        {
            DicomDataset ds = TranslationTestHelpers.BuildPrivateDataset();
            VerifyBsonTripleTrip_BlacklistedRemoved(ds);
        }

        /// <summary>
        /// Asserts that all the Dicom numeric value types are properly converted
        /// </summary>
        [Test]
        public void BsonRoundTrip_ValueTypesMinMax_SameAfterConversion()
        {
            var ds = new DicomDataset
            {
                new DicomUnsignedShort(DicomTag.Rows, ushort.MaxValue),
                new DicomFloatingPointSingle(DicomTag.SelectorFLValue, 0, float.MinValue, float.MaxValue),
                new DicomFloatingPointDouble(DicomTag.SelectorFDValue, 0, double.MinValue, double.MaxValue),
                new DicomOtherDouble(DicomTag.SelectorODValue, 0, double.MinValue, double.MaxValue),
                new DicomOtherFloat(DicomTag.FloatPixelData, 0, float.MinValue, float.MaxValue),
                new DicomOtherLong(DicomTag.SelectorOLValue, 0, 1, uint.MaxValue),
                new DicomSignedLong(DicomTag.SelectorSLValue, 0, int.MinValue, int.MaxValue),
                new DicomSignedShort(DicomTag.SelectorSSValue, 0, short.MinValue, short.MaxValue),
                new DicomUnsignedLong(DicomTag.SelectorULValue, 0, 1, uint.MaxValue),
                new DicomUnsignedShort(DicomTag.SelectorUSValue, 0, 1, ushort.MaxValue),
            };

            VerifyBsonTripleTrip_BlacklistedRemoved(ds);
        }

        [Test]
        public void BsonRoundTrip_DicomAttributeTags_SameAfterConversion()
        {
            var ds = new DicomDataset
            {
                new DicomAttributeTag(DicomTag.SelectorATValue, DicomTag.SOPInstanceUID, DicomTag.SeriesInstanceUID)
            };

            VerifyBsonTripleTrip_BlacklistedRemoved(ds);
        }

        /// <summary>
        /// <see href="http://dicom.nema.org/medical/dicom/current/output/chtml/part05/sect_7.6.html">DICOM Repeating Groups</see>
        /// </summary>
        [Test]
        public void BsonRoundTrip_MaskedTags_ConvertedCorrectly()
        {
            const string rawJson = "{\"60000010\":{\"vr\":\"US\",\"val\":[128]},\"60000011\":{\"vr\":\"US\",\"val\":[614]},\"60000040\":" +
                                   "{\"vr\":\"CS\",\"val\":\"G\"},\"60000050\":{\"vr\":\"SS\",\"val\":[0,0]},\"60000100\":{\"vr\":\"US\",\"val\":[1]}," +
                                   "\"60000102\":{\"vr\":\"US\",\"val\":[0]},\"60020010\":{\"vr\":\"US\",\"val\":[512]},\"60020011\":{\"vr\":\"US\",\"val\":[613]}," +
                                   "\"60020040\":{\"vr\":\"CS\",\"val\":\"H\"},\"60020050\":{\"vr\":\"SS\",\"val\":[1,2]},\"60020100\":{\"vr\":\"US\",\"val\":[3]}," +
                                   "\"60020102\":{\"vr\":\"US\",\"val\":[4]}}";

            DicomDataset maskDataset = DicomTypeTranslater.DeserializeJsonToDataset(rawJson);

            Assert.AreEqual(12, maskDataset.Count());

            foreach (DicomItem item in maskDataset.Where(x => x.Tag.DictionaryEntry.Keyword == DicomTag.OverlayRows.DictionaryEntry.Keyword))
                _logger.Debug("{0} {1} - Val: {2}", item.Tag, item.Tag.DictionaryEntry.Keyword, maskDataset.GetString(item.Tag));

            VerifyBsonTripleTrip_BlacklistedRemoved(maskDataset);
        }

        [Test]
        public void BsonRoundTrip_Utf8Encoding_ConvertedCorrectly()
        {
            var ds = new DicomDataset
            {
                new DicomUnlimitedText(DicomTag.TextValue, Encoding.UTF8, "¥£€$¢₡₢₣₤₥₦₧₨₩₪₫₭₮₯₹")
            };

            VerifyBsonTripleTrip_BlacklistedRemoved(ds);
        }

        /// <summary>
        /// Asserts that VRs which are in the blacklist have their values set to null when writing to BsonDocuments, and are ignored when being reconstructed back into datasets
        /// </summary>
        [Test]
        public void BsonRoundTrip_BlacklistedVrs_ConvertedCorrectly()
        {
            var ds = new DicomDataset
            {
                new DicomOtherByte(DicomTag.SelectorOBValue, byte.MinValue),
                new DicomOtherWord(DicomTag.SelectorOWValue, byte.MinValue),
                new DicomUnknown(DicomTag.SelectorUNValue, byte.MinValue)
            };

            // Ensure this test fails if we update the blacklist later
            Assert.True(
                ds.Select(x => x.ValueRepresentation).All(DicomTypeTranslater.DicomBsonVrBlacklist.Contains)
                && ds.Count() == DicomTypeTranslater.DicomBsonVrBlacklist.Length);

            BsonDocument doc = DicomTypeTranslaterReader.BuildBsonDocument(ds);

            Assert.NotNull(doc);
            Assert.True(doc.All(x => x.Value.IsBsonNull));

            DicomDataset recoDs = DicomTypeTranslaterWriter.BuildDicomDataset(doc);

            Assert.NotNull(doc);
            Assert.AreEqual(0, recoDs.Count());
        }

        [Test]
        [TestCase(null)] // Change this to test a specific VR on its own
        public void BsonRoundTrip_SingleVrs_ConvertedCorrectly(string vrString)
        {
            DicomVR vrToTest = null;

            if (!string.IsNullOrWhiteSpace(vrString))
                vrToTest = DicomVR.Parse(vrString);

            foreach (DicomVR vr in TranslationTestHelpers.AllVrCodes)
            {
                if (vrToTest != null && vr != vrToTest)
                    continue;

                DicomDataset ds = TranslationTestHelpers.BuildVrDataset(vr);

                BsonDocument document = DicomTypeTranslaterReader.BuildBsonDocument(ds);
                Assert.NotNull(document);

                DicomDataset reconstructedDataset = DicomTypeTranslaterWriter.BuildDicomDataset(document);
                Assert.NotNull(reconstructedDataset);

                if (!DicomTypeTranslater.DicomBsonVrBlacklist.Contains(vr))
                {
                    Assert.AreEqual(1, reconstructedDataset.Count());
                    Assert.True(DicomDatasetHelpers.ValueEquals(ds, reconstructedDataset));
                }
                else
                {
                    Assert.AreEqual(0, reconstructedDataset.Count());
                }
            }
        }

        #endregion
    }
}
