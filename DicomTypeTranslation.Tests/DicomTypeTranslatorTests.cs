
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Dicom;
using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.Helpers;
using NLog;
using NUnit.Framework;

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

            Assert.True(DicomDatasetHelpers.ValueEquals(originalDataset, reconstructedDataset));
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

            object result = DicomTypeTranslaterReader.GetCSharpValue(dicomDataset, DicomTag.ProcedureCodeSequence);


            object flat = DicomTypeTranslater.Flatten(result);
            Console.WriteLine(flat);

            StringAssert.Contains("CPELVD", (string)flat);
            StringAssert.Contains("(0008,0100)", (string)flat);
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


        [Test]
        public void Test_GetCSharpValue_PrivateTags()
        {
            // Create a dataset with the private tag in it
            var aTag = new DicomTag(0x3001, 0x08, "PRIVATE");
            var ds = new DicomDataset();
            ds.Add<int>(aTag, 1);

            // Getting the value directly is fine
            Assert.AreEqual(1, ds.GetSingleValue<int>(aTag));
            Assert.AreEqual(1, DicomTypeTranslaterReader.GetCSharpValue(ds, aTag));

            // Getting it by iterating through the dataset also works
            // NOTE(rkm 2020-03-26) When creating a dataset with private tags, the "Private Creator" tags are also implicitly added to the dataset
            foreach (DicomItem item in ds)
                if (item.ToString().Contains("(3001,1008)"))
                    Assert.AreEqual(1, DicomTypeTranslaterReader.GetCSharpValue(ds, item));
        }

        [Test]
        public void ShowBlacklistedTags()
        {
            List<DicomTag> blacklistedTags = typeof(DicomTag)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(DicomTag))
                .Select(field => (DicomTag)field.GetValue(null))
                .Where(tag => tag.DictionaryEntry.ValueRepresentations
                    .Any(vr => DicomTypeTranslater.DicomVrBlacklist.Contains(vr)))
                .ToList();

            Console.WriteLine($"Total blacklisted elements {blacklistedTags.Count}");

            foreach (DicomTag tag in blacklistedTags.OrderBy(x => x.DictionaryEntry.Keyword))
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
            Assert.AreEqual(34, TranslationTestHelpers.AllVrCodes.Length);
        }

        [Test]
        [TestCase("SV")]
        [TestCase("UV")]
        public void TestVrsWithNoTags(string vrName)
        {
            DicomVR vr = DicomVR.Parse(vrName);

            // NOTE(rkm 2020-03-25) When this fails, add an entry for the new VR to the datasets TranslationTestHelpers
            List<DicomTag> vrTags = typeof(DicomTag)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(field => field.FieldType == typeof(DicomTag))
                .Select(x => (DicomTag)x.GetValue(null))
                .Where(tag => tag.DictionaryEntry.ValueRepresentations[0] == vr)
                .ToList();

            Assert.AreEqual(0, vrTags.Count);
        }

        #endregion
    }
}
