
using System;
using System.IO;
using System.Linq;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.Helpers;

using MongoDB.Bson;

using NLog;

using NUnit.Framework;


namespace DicomTypeTranslation.Tests;

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

        var document = DicomTypeTranslaterReader.BuildBsonDocument(dataset);

        Assert.Multiple(() =>
        {
            Assert.That(document.Count(), Is.EqualTo(2));
            Assert.That(document[0], Is.EqualTo(BsonNull.Value));
            Assert.That(document[1], Is.EqualTo(BsonNull.Value));
        });
    }

    [Test]
    public void DicomToBson_TranslateNormalDataset_DoesNotThrow()
    {
        Assert.DoesNotThrow(static () => DicomTypeTranslaterReader.BuildBsonDocument(TranslationTestHelpers.BuildVrDataset()));
    }

    [Test]
    public void DicomToBson_TranslatePrivateDataset_DoesNotThrow()
    {
        var ds = TranslationTestHelpers.BuildPrivateDataset();
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
        var privateCreator = DicomDictionary.Default.GetPrivateCreator("TEST");
        var pDict = DicomDictionary.Default[privateCreator];

        pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx02"), "Private Tag 02", "PrivateTag02", DicomVM.VM_1, false, DicomVR.AE));

        var ds = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, "1.2.3.4" }
        };

        ds.Add(new DicomApplicationEntity(ds.GetPrivateTag(new DicomTag(3, 0x0002, privateCreator)), "AETITLE"));

        var bsonDoc = DicomTypeTranslaterReader.BuildBsonDocument(ds);

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

        Assert.That(bsonDoc, Is.EqualTo(expected));
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
                    new DicomUnsignedShort(new DicomTag(1953, 4176, "ELSCINT1"), 123)
                },
                new DicomDataset
                {
                    new DicomCodeString(new DicomTag(1953, 16), "ELSCINT2"),
                    new DicomUnsignedShort(new DicomTag(1953, 4176, "ELSCINT2"), 456)
                }
            },
            { DicomVR.US, DicomTag.GrayLookupTableDataRETIRED, ushort.MinValue, ushort.MaxValue },
            new DicomCodeString(new DicomTag(1953, 16), "ELSCINT3"),
            new DicomUnsignedShort(new DicomTag(1953, 4176, "ELSCINT3"), 789)
        };

        var convertedDoc = DicomTypeTranslaterReader.BuildBsonDocument(ds);

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

        Assert.That(convertedDoc, Is.EqualTo(expectedDoc));
    }

    /// <summary>
    /// Asserts that group length elements (xxxx,0000) are ignored from BSON serialization
    /// </summary>
    [Test]
    public void DicomToBson_GroupLengthElements_AreIgnored()
    {
        var ds = new DicomDataset
        {
            new DicomCodeString(new DicomTag(123,0), "ABC")
        };

        Assert.That(DicomTypeTranslaterReader.BuildBsonDocument(ds), Is.Empty);
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

        var actual = DicomTypeTranslaterReader.BuildBsonDocument(ds);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void DicomToBson_EmptyElements_StoredAsBsonNull()
    {
        var ds = new DicomDataset
        {
            new DicomAttributeTag(DicomTag.SelectorATValue),
            new DicomLongString(DicomTag.SelectorLOValue),
            new DicomOtherLong(DicomTag.SelectorOLValue),
            new DicomOtherWord(DicomTag.SelectorOWValue),
            new DicomSequence(DicomTag.SelectorCodeSequenceValue)
        };

        var doc = DicomTypeTranslaterReader.BuildBsonDocument(ds);

        Assert.That(doc.Count(), Is.EqualTo(ds.Count()));

        foreach (var element in doc)
            Assert.That(element.Value.IsBsonNull, Is.True);
    }

    [Test]
    public void DicomToBson_EmptyPrivateElements_StoredAsBsonNull()
    {
        var ds = TranslationTestHelpers.BuildAllTypesNullDataset();
        var doc = DicomTypeTranslaterReader.BuildBsonDocument(ds);

        Assert.That(doc.Count(), Is.EqualTo(ds.Count()));

        foreach (var element in doc)
        {
            var asBsonDoc = element.Value.AsBsonDocument;
            Assert.That(asBsonDoc, Is.Not.Null);

            Assert.That(asBsonDoc.GetValue("val").IsBsonNull, Is.True); // Private elements
        }
    }

    #endregion
}

[TestFixture]
public class BsonToDicomTranslationTests
{
    #region Tests

    /// <summary>
    /// Tests that if the VR of a multi-VR tag isn't specified in the document, then the
    /// exception message contains enough data to debug the specific tag causing the issue.
    /// </summary>
    [Test]
    public void BsonToDicom_UnspecifiedMultiVrTag_ThrowsWithInfoMessage()
    {
        var doc = new BsonDocument
        {
            { "DarkCurrentCounts", new BsonArray() }
        };

        Assert.Throws<InvalidOperationException>(() =>
        {
            try
            {
                DicomTypeTranslaterWriter.BuildDicomDataset(doc);
            }
            catch (InvalidOperationException e)
            {
                Assert.That(e.Data["DicomTag"], Is.EqualTo("DarkCurrentCounts"));
                throw;
            }
        });
    }

    [Test]
    public void BsonToDicom_UnknownPrivateSequence_DoesNotThrow()
    {
        var doc = new BsonDocument
        {
            { "(7015,1173:PMTF INFORMATION DATA)-Unknown", new BsonArray
                { new BsonDocument
                    {
                        { "(0029,0010)-PrivateCreator", new BsonDocument
                            {
                                { "vr", "LO" },
                                { "val", "aaah" }
                            }
                        },
                        { "(0029,1090:PMTF INFORMATION DATA)-Unknown", new BsonDocument
                            {
                                { "vr", "OB" },
                                { "val", BsonNull.Value }
                            }
                        }
                    }
                }
            }
        };

        Assert.DoesNotThrow(() => DicomTypeTranslaterWriter.BuildDicomDataset(doc));
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

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        TestLogger.ShutDown();
    }

    #endregion

    #region Test Helpers

    private static void VerifyBsonTripleTrip(DicomDataset ds)
    {
        var bsonDoc = DicomTypeTranslaterReader.BuildBsonDocument(ds);
        var recoDataset = DicomTypeTranslaterWriter.BuildDicomDataset(bsonDoc);
        var recoDoc = DicomTypeTranslaterReader.BuildBsonDocument(recoDataset);

        Assert.Multiple(() =>
        {
            Assert.That(recoDoc, Is.EqualTo(bsonDoc));
            Assert.That(DicomDatasetHelpers.ValueEquals(ds, recoDataset), Is.True);
        });
    }

    #endregion

    #region Tests

    [Ignore("Ignore unless you want to test specific files")]
    [TestCase("")]
    public void BsonRoundTrip_TestFile_PassesConversion(string filePath)
    {
        Assert.That(File.Exists(filePath), Is.True);
        var ds = DicomFile.Open(filePath).Dataset;
        VerifyBsonTripleTrip(ds);
    }

    [Test]
    public void BsonRoundTrip_SimpleTag_SameAfterConversion()
    {
        var ds = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, "1.3.6.1.4.1.9328.50.51.26748523322000548" }
        };

        VerifyBsonTripleTrip(ds);
    }

    [Test]
    public void BsonRoundTrip_TagWithMultipleVrs_SameAfterConversion()
    {
        DicomTypeTranslater.SerializeBinaryData = true;

        var usItem = new DicomUnsignedShort(DicomTag.GrayLookupTableDataRETIRED, 0, 1, ushort.MaxValue);
        var owItem = new DicomOtherWord(DicomTag.GrayLookupTableDataRETIRED, 0, 1, ushort.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(usItem.Tag.DictionaryEntry.ValueRepresentations, Has.Length.EqualTo(3));
            Assert.That(owItem.Tag.DictionaryEntry.ValueRepresentations, Has.Length.EqualTo(3));
        });

        var usDataset = new DicomDataset { usItem };
        var owDataset = new DicomDataset { owItem };

        var usDocument = DicomTypeTranslaterReader.BuildBsonDocument(usDataset);
        var owDocument = DicomTypeTranslaterReader.BuildBsonDocument(owDataset);

        Assert.Multiple(() =>
        {
            Assert.That(usDocument, Is.Not.Null);
            Assert.That(owDocument, Is.Not.Null);
        });

        var recoUsDataset = DicomTypeTranslaterWriter.BuildDicomDataset(usDocument);
        var recoOwDataset = DicomTypeTranslaterWriter.BuildDicomDataset(owDocument);

        Assert.Multiple(() =>
        {
            Assert.That(DicomDatasetHelpers.ValueEquals(usDataset, recoUsDataset), Is.True);
            Assert.That(DicomDatasetHelpers.ValueEquals(owDataset, recoOwDataset), Is.True);
        });
    }

    [Test]
    public void BsonRoundTrip_NormalPrivateDataset_SameAfterConversion()
    {
        var ds = TranslationTestHelpers.BuildPrivateDataset();
        ds = new DicomDataset(ds.Where(x => !DicomTypeTranslater.DicomVrBlacklist.Contains(x.ValueRepresentation)));
        VerifyBsonTripleTrip(ds);
    }

    [Test]
    public void BsonRoundTrip_DicomUnsignedVeryLong_SameAfterConversion()
    {
        var privateCreator = DicomDictionary.Default.GetPrivateCreator("TEST");
        var pDict = DicomDictionary.Default[privateCreator];
        pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx01"), "Private Tag 01", "PrivateTag01", DicomVM.VM_1, false, DicomVR.UV));
        pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx02"), "Private Tag 02", "PrivateTag02", DicomVM.VM_1, false, DicomVR.UV));
        pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx03"), "Private Tag 03", "PrivateTag03", DicomVM.VM_1, false, DicomVR.UV));

        var ds = new DicomDataset();
        ds.Add(new DicomUnsignedVeryLong(ds.GetPrivateTag(new DicomTag(3, 0x0001, privateCreator)), 0x0));
        ds.Add(new DicomUnsignedVeryLong(ds.GetPrivateTag(new DicomTag(3, 0x0002, privateCreator)), 0xffffffffffffffff - 123));
        ds.Add(new DicomUnsignedVeryLong(ds.GetPrivateTag(new DicomTag(3, 0x0003, privateCreator)), 0xffffffffffffffff));

        ds = new DicomDataset(ds.Where(x => !DicomTypeTranslater.DicomVrBlacklist.Contains(x.ValueRepresentation)));
        VerifyBsonTripleTrip(ds);
    }

    [Test]
    public void BsonRoundTrip_BlacklistPrivateDataset_ZeroAfterConversion()
    {
        var ds = TranslationTestHelpers.BuildPrivateDataset();
        ds = new DicomDataset(ds.Where(x => DicomTypeTranslater.DicomVrBlacklist.Contains(x.ValueRepresentation)));
        var doc = DicomTypeTranslaterReader.BuildBsonDocument(ds);
        var recoDataset = DicomTypeTranslaterWriter.BuildDicomDataset(doc);
        var recoDoc = DicomTypeTranslaterReader.BuildBsonDocument(recoDataset);

        Assert.That(recoDoc, Is.EqualTo(doc));

        foreach (var element in recoDataset.Select(x => (DicomElement)x))
            Assert.That(element.Buffer.Size, Is.Zero);
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
            new DicomUnsignedShort(DicomTag.SelectorUSValue, 0, 1, ushort.MaxValue)
        };

        VerifyBsonTripleTrip(ds);
    }

    [Test]
    public void BsonRoundTrip_DicomAttributeTags_SameAfterConversion()
    {
        var ds = new DicomDataset
        {
            new DicomAttributeTag(DicomTag.SelectorATValue, DicomTag.SOPInstanceUID, DicomTag.SeriesInstanceUID)
        };

        VerifyBsonTripleTrip(ds);
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

        var maskDataset = DicomTypeTranslater.DeserializeJsonToDataset(rawJson,true);

        Assert.That(maskDataset.Count(), Is.EqualTo(12));

        foreach (var item in maskDataset.Where(x => x.Tag.DictionaryEntry.Keyword == DicomTag.OverlayRows.DictionaryEntry.Keyword))
            _logger.Debug("{0} {1} - Val: {2}", item.Tag, item.Tag.DictionaryEntry.Keyword, maskDataset.GetString(item.Tag));

        VerifyBsonTripleTrip(maskDataset);
    }

    [Test]
    public void BsonRoundTrip_Utf8Encoding_ConvertedCorrectly()
    {
        var ds = new DicomDataset
        {
            new DicomUnlimitedText(DicomTag.TextValue, "¥£€$¢₡₢₣₤₥₦₧₨₩₪₫₭₮₯₹")
        };

        VerifyBsonTripleTrip(ds);
    }

    /// <summary>
    /// Asserts that VRs which are in the blacklist have their values set to null when writing to BsonDocuments, and are ignored when being reconstructed back into datasets
    /// </summary>
    [Test]
    public void BsonRoundTrip_BlacklistedVrs_ConvertedCorrectly()
    {
        var ds = new DicomDataset
        {
            new DicomOtherByte(DicomTag.SelectorOBValue, byte.MaxValue),
            new DicomOtherVeryLong(DicomTag.ExtendedOffsetTable, ulong.MaxValue),
            new DicomOtherWord(DicomTag.SelectorOWValue, byte.MaxValue),
            new DicomUnknown(DicomTag.SelectorUNValue, byte.MaxValue)
        };

        // Ensure this test fails if we update the blacklist later
        Assert.That(
            ds.Select(x => x.ValueRepresentation).All(DicomTypeTranslater.DicomVrBlacklist.Contains)
            && ds.Count() == DicomTypeTranslater.DicomVrBlacklist.Length, Is.True);

        var doc = DicomTypeTranslaterReader.BuildBsonDocument(ds);

        Assert.That(doc, Is.Not.Null);
        Assert.That(doc.All(x => x.Value.IsBsonNull), Is.True);

        var recoDs = DicomTypeTranslaterWriter.BuildDicomDataset(doc);

        Assert.That(recoDs.Count(), Is.EqualTo(4));
        foreach (var item in recoDs)
            Assert.That(((DicomElement)item).Count, Is.Zero);
    }

    [Test]
    public void BsonRoundTrip_NormalDataset_SameAfterConversion()
    {
        var ds = new DicomDataset(TranslationTestHelpers.BuildZooDataset()
            .Where(x => !DicomTypeTranslater.DicomVrBlacklist.Contains(x.ValueRepresentation)));

        VerifyBsonTripleTrip(ds);
    }

    [Test]
    public void BsonRoundTrip_EmptyElements_ConvertedCorrectly()
    {
        var ds = new DicomDataset
        {
            new DicomAttributeTag(DicomTag.SelectorATValue),    // DicomAttributeTag
            new DicomCodeString(DicomTag.SelectorCSValue),      // DicomMultiStringElement
            new DicomLongText(DicomTag.SelectorLTValue, EmptyBuffer.Value.ToString()),  // DicomStringElement
            new DicomOtherLong(DicomTag.SelectorOLValue),       // DicomValueElement<uint>
            new DicomOtherWord(DicomTag.SelectorOWValue),       // DicomValueElement<ushort>
            new DicomSequence(DicomTag.SelectorCodeSequenceValue) // DicomSequence
        };

        VerifyBsonTripleTrip(ds);
    }

    [Test]
    public void BsonRoundTrip_EmptyPrivateElements_ConvertedCorrectly()
    {
        var ds = TranslationTestHelpers.BuildAllTypesNullDataset();
        VerifyBsonTripleTrip(ds);
    }

    [Test]
    public void BsonRoundTrip_StringEncoding_ConvertedCorrectly()
    {
        var ds = new DicomDataset
        {
            new DicomShortString(DicomTag.SelectorSHValue, "simples"),
            new DicomLongString(DicomTag.SelectorLOValue, "(╯°□°）╯︵ ┻━┻")
        };

        var recoDs = DicomTypeTranslaterWriter.BuildDicomDataset(DicomTypeTranslaterReader.BuildBsonDocument(ds));

        foreach (var stringElement in recoDs.Select(x => x as DicomStringElement))
        {
            Assert.That(stringElement, Is.Not.Null);
        }
    }

    /// <summary>
    /// Tests that private sequence elements are handled properly when the private creator mapping isn't known
    /// </summary>
    [Test]
    public void BsonRoundTrip_UnknownPrivateSequence_ConvertedCorrectly()
    {
        var privateCreator = DicomDictionary.Default.GetPrivateCreator("???");

        var ds = new DicomDataset();
        ds.Add(new DicomSequence(ds.GetPrivateTag(new DicomTag(3, 0x0017, privateCreator)),
            new DicomDataset { new DicomShortText(new DicomTag(3, 0x0018, privateCreator), "ಠ_ಠ") }));

        VerifyBsonTripleTrip(ds);
    }

    #endregion
}