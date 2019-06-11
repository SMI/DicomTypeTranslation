
using Dicom;
using Dicom.IO.Buffer;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DicomTypeTranslation.Tests.Helpers
{
    public static class TranslationTestHelpers
    {
        // All VR codes in current dicom standard
        public static readonly DicomVR[] AllVrCodes = typeof(DicomVR)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(field => field.FieldType == typeof(DicomVR))
            .Select(field => (DicomVR)field.GetValue(null))
            .Where(vr => vr != DicomVR.NONE)
            .ToArray();


        public static DicomDataset BuildVrDataset(DicomVR singleVr = null)
        {
            var ds = new DicomDataset
            {
                new DicomApplicationEntity(DicomTag.RetrieveAETitle, "ApplicationEntity-1", "ApplicationEntity-2"),
                new DicomAgeString(DicomTag.PatientAge, "34y", "32y"),
                new DicomAttributeTag(DicomTag.SelectorATValue, DicomTag.SOPInstanceUID, DicomTag.SeriesInstanceUID),
                new DicomCodeString(DicomTag.QualityControlImage, "FOOBAR", "OOFRAB"),
                new DicomDate(DicomTag.AcquisitionDate, "20000229", "20180401"),
                new DicomDecimalString(DicomTag.LossyImageCompressionRatio, "0", "123.456"),
                new DicomDateTime(DicomTag.AcquisitionDateTime, "20141231194212", "20180401235959"),
                new DicomFloatingPointSingle(DicomTag.SelectorFLValue, 0, float.MinValue, float.MaxValue),
                new DicomFloatingPointDouble(DicomTag.SelectorFDValue, 0, double.MinValue, double.MaxValue),
                new DicomIntegerString(DicomTag.ImagesInAcquisition, 0, int.MinValue, int.MaxValue),
                new DicomLongString(DicomTag.PatientState, "This is a long string part 1", "This is a long string part 2"),
                new DicomLongText(DicomTag.AdditionalPatientHistory, @"This is a dicom long string. Backslashes should be ok: \\\\\\"),
                new DicomOtherByte(DicomTag.SelectorOBValue, 1, 2, 3, 0, 255),
                new DicomOtherDouble(DicomTag.SelectorODValue, 0, double.MinValue, double.MaxValue),
                new DicomOtherFloat(DicomTag.FloatPixelData, 0, float.MinValue, float.MaxValue),
                new DicomOtherLong(DicomTag.SelectorOLValue, 0, 1, uint.MaxValue),
                new DicomOtherWord(DicomTag.SelectorOWValue, 0, 1, ushort.MaxValue),
                new DicomPersonName(DicomTag.SelectorPNValue, new [] { "Morrison-Jones^Susan^^^Ph.D.1", "Morrison-Jones^Susan^^^Ph.D.2"}),
                new DicomShortString(DicomTag.SelectorSHValue, "ShortString-1", "Short-String-2"),
                new DicomSignedLong(DicomTag.SelectorSLValue, 0, int.MinValue, int.MaxValue),
                new DicomSequence(DicomTag.AdmittingDiagnosesCodeSequence, new DicomDataset {new DicomShortText(DicomTag.SelectorSTValue, "Short\\Text"), new DicomIntegerString(DicomTag.ImagesInAcquisition, "1234")}),
                new DicomSignedShort(DicomTag.SelectorSSValue, 0, short.MinValue, short.MaxValue),
                new DicomShortText(DicomTag.SelectorSTValue, "Short\\Text\\Backslashes should be ok: \\\\\\"),
                new DicomTime(DicomTag.SelectorTMValue, "123456", "235959"),
                new DicomUnlimitedCharacters(DicomTag.SelectorUCValue, "UnlimitedCharacters-1"),
                new DicomUniqueIdentifier(DicomTag.SelectorUIValue, "1.2.3.4", "5.6.7.8"),
                new DicomUnsignedLong(DicomTag.SelectorULValue, 0, 1, uint.MaxValue),
                new DicomUnknown(DicomTag.SelectorUNValue, 0, 1, 255),
                new DicomUniversalResource(DicomTag.URNCodeValue, "http://example.com?q=1"),
                new DicomUnsignedShort(DicomTag.SelectorUSValue, 0, 1, ushort.MaxValue),
                new DicomUnlimitedText(DicomTag.SelectorUTValue, "unlimited!")
            };

            if (singleVr != null)
                ds.Remove(x => x.ValueRepresentation != singleVr);

            return ds;
        }

        public static DicomDataset BuildPrivateDataset()
        {
            DicomPrivateCreator privateCreator = DicomDictionary.Default.GetPrivateCreator("TEST");
            DicomDictionary pDict = DicomDictionary.Default[privateCreator];

            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx02"), "Private Tag 02", "PrivateTag02", DicomVM.VM_1, false, DicomVR.AE));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx03"), "Private Tag 03", "PrivateTag03", DicomVM.VM_1, false, DicomVR.AS));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx04"), "Private Tag 04", "PrivateTag04", DicomVM.VM_1, false, DicomVR.AT));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx05"), "Private Tag 05", "PrivateTag05", DicomVM.VM_1, false, DicomVR.CS));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx06"), "Private Tag 06", "PrivateTag06", DicomVM.VM_1, false, DicomVR.DA));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx07"), "Private Tag 07", "PrivateTag07", DicomVM.VM_1, false, DicomVR.DS));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx08"), "Private Tag 08", "PrivateTag08", DicomVM.VM_1, false, DicomVR.DT));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx09"), "Private Tag 09", "PrivateTag09", DicomVM.VM_1, false, DicomVR.FL));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0a"), "Private Tag 0a", "PrivateTag0a", DicomVM.VM_1, false, DicomVR.FD));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0b"), "Private Tag 0b", "PrivateTag0b", DicomVM.VM_1, false, DicomVR.IS));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0c"), "Private Tag 0c", "PrivateTag0c", DicomVM.VM_1, false, DicomVR.LO));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0d"), "Private Tag 0d", "PrivateTag0d", DicomVM.VM_1, false, DicomVR.LT));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0e"), "Private Tag 0e", "PrivateTag0e", DicomVM.VM_1, false, DicomVR.OB));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0f"), "Private Tag 0f", "PrivateTag0f", DicomVM.VM_1, false, DicomVR.OD));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx10"), "Private Tag 10", "PrivateTag10", DicomVM.VM_1, false, DicomVR.OF));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx11"), "Private Tag 11", "PrivateTag11", DicomVM.VM_1, false, DicomVR.OL));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx12"), "Private Tag 12", "PrivateTag12", DicomVM.VM_1, false, DicomVR.OW));
            //pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx13"), "Private Tag 13", "PrivateTag13", DicomVM.VM_1, false, DicomVR.OV));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx14"), "Private Tag 14", "PrivateTag14", DicomVM.VM_1, false, DicomVR.PN));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx15"), "Private Tag 15", "PrivateTag15", DicomVM.VM_1, false, DicomVR.SH));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx16"), "Private Tag 16", "PrivateTag16", DicomVM.VM_1, false, DicomVR.SL));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx17"), "Private Tag 17", "PrivateTag17", DicomVM.VM_1, false, DicomVR.SQ));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx18"), "Private Tag 18", "PrivateTag18", DicomVM.VM_1, false, DicomVR.ST));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx19"), "Private Tag 19", "PrivateTag19", DicomVM.VM_1, false, DicomVR.SS));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1a"), "Private Tag 1a", "PrivateTag1a", DicomVM.VM_1, false, DicomVR.ST));
            //pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1b"), "Private Tag 1b", "PrivateTag1b", DicomVM.VM_1, false, DicomVR.SV));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1c"), "Private Tag 1c", "PrivateTag1c", DicomVM.VM_1, false, DicomVR.TM));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1d"), "Private Tag 1d", "PrivateTag1d", DicomVM.VM_1, false, DicomVR.UC));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1e"), "Private Tag 1e", "PrivateTag1e", DicomVM.VM_1, false, DicomVR.UI));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1f"), "Private Tag 1f", "PrivateTag1f", DicomVM.VM_1, false, DicomVR.UL));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx20"), "Private Tag 20", "PrivateTag20", DicomVM.VM_1, false, DicomVR.UN));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx21"), "Private Tag 21", "PrivateTag21", DicomVM.VM_1, false, DicomVR.UR));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx22"), "Private Tag 22", "PrivateTag22", DicomVM.VM_1, false, DicomVR.US));
            pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx23"), "Private Tag 23", "PrivateTag23", DicomVM.VM_1, false, DicomVR.UT));
            //pDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx24"), "Private Tag 24", "PrivateTag24", DicomVM.VM_1, false, DicomVR.UV));

            var ds = new DicomDataset();

            ds.Add(new DicomApplicationEntity(ds.GetPrivateTag(new DicomTag(3, 0x0002, privateCreator)), "AETITLE"));
            ds.Add(new DicomAgeString(ds.GetPrivateTag(new DicomTag(3, 0x0003, privateCreator)), "034Y"));
            ds.Add(new DicomAttributeTag(ds.GetPrivateTag(new DicomTag(3, 0x0004, privateCreator)), new[] { DicomTag.SOPInstanceUID }));
            ds.Add(new DicomCodeString(ds.GetPrivateTag(new DicomTag(3, 0x0005, privateCreator)), "FOOBAR"));
            ds.Add(new DicomDate(ds.GetPrivateTag(new DicomTag(3, 0x0006, privateCreator)), "20000229"));
            ds.Add(new DicomDecimalString(ds.GetPrivateTag(new DicomTag(3, 0x0007, privateCreator)), new[] { "9876543210123457" }));
            ds.Add(new DicomDateTime(ds.GetPrivateTag(new DicomTag(3, 0x0008, privateCreator)), "20141231194212"));
            ds.Add(new DicomFloatingPointSingle(ds.GetPrivateTag(new DicomTag(3, 0x0009, privateCreator)), new[] { 0.25f }));
            ds.Add(new DicomFloatingPointDouble(ds.GetPrivateTag(new DicomTag(3, 0x000a, privateCreator)), new[] { Math.PI }));
            ds.Add(new DicomIntegerString(ds.GetPrivateTag(new DicomTag(3, 0x000b, privateCreator)), 2147483647));
            ds.Add(new DicomLongString(ds.GetPrivateTag(new DicomTag(3, 0x000c, privateCreator)), "(╯°□°）╯︵ ┻━┻"));
            ds.Add(new DicomLongText(ds.GetPrivateTag(new DicomTag(3, 0x000d, privateCreator)), "┬──┬ ノ( ゜-゜ノ)"));
            ds.Add(new DicomOtherByte(ds.GetPrivateTag(new DicomTag(3, 0x000e, privateCreator)), new byte[] { 1, 2, 3, 0, 255 }));
            ds.Add(new DicomOtherDouble(ds.GetPrivateTag(new DicomTag(3, 0x000f, privateCreator)), new double[] { 1.0, 2.5 }));
            ds.Add(new DicomOtherFloat(ds.GetPrivateTag(new DicomTag(3, 0x0010, privateCreator)), new float[] { 1.0f, 2.9f }));
            ds.Add(new DicomOtherLong(ds.GetPrivateTag(new DicomTag(3, 0x0011, privateCreator)), new uint[] { 0xffffffff, 0x00000000, 0x12345678 }));
            ds.Add(new DicomOtherWord(ds.GetPrivateTag(new DicomTag(3, 0x0012, privateCreator)), new ushort[] { 0xffff, 0x0000, 0x1234 }));
            //ds.Add(new DicomOtherVeryLong(ds.GetPrivateTag(new DicomTag(3, 0x0013, privateCreator)), new ulong[] { ulong.MaxValue, ulong.MinValue, 0x1234 }));
            ds.Add(new DicomPersonName(ds.GetPrivateTag(new DicomTag(3, 0x0014, privateCreator)), "Morrison-Jones^Susan^^^Ph.D."));
            ds.Add(new DicomShortString(ds.GetPrivateTag(new DicomTag(3, 0x0015, privateCreator)), "顔文字"));
            ds.Add(new DicomSignedLong(ds.GetPrivateTag(new DicomTag(3, 0x0016, privateCreator)), -65538));
            ds.Add(new DicomSequence(ds.GetPrivateTag(new DicomTag(3, 0x0017, privateCreator)), new[] { new DicomDataset { new DicomShortText(new DicomTag(3, 0x0018, privateCreator), "ಠ_ಠ") } }));
            ds.Add(new DicomSignedShort(ds.GetPrivateTag(new DicomTag(3, 0x0019, privateCreator)), -32768));
            ds.Add(new DicomShortText(ds.GetPrivateTag(new DicomTag(3, 0x001a, privateCreator)), "ಠ_ಠ"));
            //ds.Add(new DicomSignedVeryLong(ds.GetPrivateTag(new DicomTag(3, 0x001b, privateCreator)), -12345678));
            ds.Add(new DicomTime(ds.GetPrivateTag(new DicomTag(3, 0x001c, privateCreator)), "123456"));
            ds.Add(new DicomUnlimitedCharacters(ds.GetPrivateTag(new DicomTag(3, 0x001d, privateCreator)), "Hmph."));
            ds.Add(new DicomUniqueIdentifier(ds.GetPrivateTag(new DicomTag(3, 0x001e, privateCreator)), DicomUID.CTImageStorage));
            ds.Add(new DicomUnsignedLong(ds.GetPrivateTag(new DicomTag(3, 0x001f, privateCreator)), 0xffffffff));
            ds.Add(new DicomUnknown(ds.GetPrivateTag(new DicomTag(3, 0x0020, privateCreator)), new byte[] { 1, 2, 3, 0, 255 }));
            ds.Add(new DicomUniversalResource(ds.GetPrivateTag(new DicomTag(3, 0x0021, privateCreator)), "http://example.com?q=1"));
            ds.Add(new DicomUnsignedShort(ds.GetPrivateTag(new DicomTag(3, 0x0022, privateCreator)), 0xffff));
            ds.Add(new DicomUnlimitedText(ds.GetPrivateTag(new DicomTag(3, 0x0023, privateCreator)), "unlimited!"));
            //ds.Add(new DicomUnsignedVeryLong(ds.GetPrivateTag(new DicomTag(3, 0x0024, privateCreator)), 0xffffffffffffffff));

            return ds;
        }

        public static DicomDataset BuildAllTypesNullDataset()
        {
            DicomPrivateCreator privateCreator = DicomDictionary.Default.GetPrivateCreator("TEST");
            return new DicomDataset {
                           new DicomApplicationEntity(new DicomTag(3, 0x1002, privateCreator)),
                           new DicomAgeString(new DicomTag(3, 0x1003, privateCreator)),
                           new DicomAttributeTag(new DicomTag(3, 0x1004, privateCreator)),
                           new DicomCodeString(new DicomTag(3, 0x1005, privateCreator)),
                           new DicomDate(new DicomTag(3, 0x1006, privateCreator), new string[0]),
                           new DicomDecimalString(new DicomTag(3, 0x1007, privateCreator), new string[0]),
                           new DicomDateTime(new DicomTag(3, 0x1008, privateCreator), new string[0]),
                           new DicomFloatingPointSingle(new DicomTag(3, 0x1009, privateCreator)),
                           new DicomFloatingPointDouble(new DicomTag(3, 0x100a, privateCreator)),
                           new DicomIntegerString(new DicomTag(3, 0x100b, privateCreator), new string[0]),
                           new DicomLongString(new DicomTag(3, 0x100c, privateCreator)),
                           new DicomLongText(new DicomTag(3, 0x100d, privateCreator), null),
                           new DicomOtherByte(new DicomTag(3, 0x100e, privateCreator), new byte[0]),
                           new DicomOtherDouble(new DicomTag(3, 0x100f, privateCreator), new double[0]),
                           new DicomOtherFloat(new DicomTag(3, 0x1010, privateCreator), new float[0]),
                           new DicomOtherLong(new DicomTag(3, 0x1014, privateCreator), new uint[0]),
                           new DicomOtherWord(new DicomTag(3, 0x1011, privateCreator), new ushort[0]),
                           new DicomPersonName(new DicomTag(3, 0x1012, privateCreator)),
                           new DicomShortString(new DicomTag(3, 0x1013, privateCreator)),
                           new DicomSignedLong(new DicomTag(3, 0x1001, privateCreator)),
                           new DicomSequence(new DicomTag(3, 0x1015, privateCreator)),
                           new DicomSignedShort(new DicomTag(3, 0x1017, privateCreator)),
                           new DicomShortText(new DicomTag(3, 0x1018, privateCreator), null),
                           new DicomTime(new DicomTag(3, 0x1019, privateCreator), new string[0]),
                           new DicomUnlimitedCharacters(new DicomTag(3, 0x101a, privateCreator), (string)null),
                           new DicomUniqueIdentifier(new DicomTag(3, 0x101b, privateCreator), new string[0]),
                           new DicomUnsignedLong(new DicomTag(3, 0x101c, privateCreator)),
                           new DicomUnknown(new DicomTag(3, 0x101d, privateCreator)),
                           new DicomUniversalResource(new DicomTag(3, 0x101e, privateCreator), null),
                           new DicomUnsignedShort(new DicomTag(3, 0x101f, privateCreator)),
                           new DicomUnlimitedText(new DicomTag(3, 0x1020, privateCreator), null)
                         };
        }

        public static DicomDataset BuildZooDataset()
        {
            var target = new DicomDataset
            {
                new DicomPersonName(DicomTag.PatientName, new[] {"Anna^Pelle", null, "Olle^Jöns^Pyjamas"}),
                {DicomTag.SOPClassUID, DicomUID.RTPlanStorage},
                {DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID()},
                {DicomTag.SeriesInstanceUID, new DicomUID[] { }},
                {DicomTag.DoseType, "HEJ"},
                {DicomTag.ControlPointSequence, (DicomSequence[]) null}
            };

            List<DicomDataset> beams = new[] { 1, 2, 3 }.Select(beamNumber =>
            {
                var beam = new DicomDataset
                {
                    { DicomTag.BeamNumber, beamNumber },
                    { DicomTag.BeamName, $"Beam #{beamNumber}" }
                };
                return beam;
            }).ToList();

            beams.Insert(1, null);
            target.Add(DicomTag.BeamSequence, beams.ToArray());

            return target;
        }

        /// <summary>
        /// Returns true if the elements in <paramref name="a"/> are the same set of tags and values as <paramref name="b"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool ValueEquals(DicomDataset a, DicomDataset b)
        {
            if (a == null || b == null)
                return a == b;

            if (a == b)
                return true;

            return a.Zip(b, ValueEquals).All(x => x);
        }


        /// <summary>
        /// Returns true if the <paramref name="a"/> contains an equal value to <paramref name="b"/> (includes support for <see cref="DicomSequence"/>)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool ValueEquals(DicomItem a, DicomItem b)
        {
            if (a == null || b == null)
                return a == b;

            if (a == b)
                return true;

            if (a.ValueRepresentation != b.ValueRepresentation || (uint)a.Tag != (uint)b.Tag)
                return false;

            if (a is DicomElement)
            {
                if (b is DicomElement == false)
                    return false;

                return ValueEquals(((DicomElement)a).Buffer, ((DicomElement)b).Buffer);
            }

            if (a is DicomSequence)
            {
                if (b is DicomSequence == false)
                    return false;

                return ((DicomSequence)a).Items.Zip(((DicomSequence)b).Items, ValueEquals).All(x => x);
            }

            if (a is DicomFragmentSequence)
            {
                if (b is DicomFragmentSequence == false)
                    return false;

                return ((DicomFragmentSequence)a).Fragments.Zip(((DicomFragmentSequence)b).Fragments, ValueEquals).All(x => x);
            }

            return a.Equals(b);
        }

        /// <summary>
        /// Returns true if <paramref name="a"/> and <paramref name="b"/> are equal.  Supports <see cref="IBulkDataUriByteBuffer"/>, <see cref="EmptyBuffer"/>, 
        /// <see cref="StreamByteBuffer"/> and <see cref="CompositeByteBuffer"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool ValueEquals(IByteBuffer a, IByteBuffer b)
        {
            if (a == null || b == null)
                return a == b;

            if (a == b)
                return true;

            if (a.IsMemory)
                return b.IsMemory && a.Data.SequenceEqual(b.Data);

            if (a is IBulkDataUriByteBuffer abuff)
            {
                if (!(b is IBulkDataUriByteBuffer bbuff))
                    return false;

                return abuff.BulkDataUri == bbuff.BulkDataUri;
            }

            if (a is EmptyBuffer && b is EmptyBuffer)
                return true;

            if (a is StreamByteBuffer && b is StreamByteBuffer)
            {
                var asbb = (StreamByteBuffer)a;
                var bsbb = (StreamByteBuffer)b;

                if (asbb.Stream == null || bsbb.Stream == null)
                    return asbb.Stream == bsbb.Stream;

                return asbb.Position == bsbb.Position && asbb.Size == bsbb.Size && asbb.Stream.Equals(bsbb.Stream);
            }

            if (a is CompositeByteBuffer && b is CompositeByteBuffer)
                return ((CompositeByteBuffer)a).Buffers.Zip(((CompositeByteBuffer)b).Buffers, ValueEquals).All(x => x);

            return a.Equals(b);
        }

        /// <summary>
        /// Compares two <see cref="DicomDataset"/>s for differences, taking the first input as the source for comparison.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ignoreTrailingNull">If set, any differences due to a single trailing NUL character will be ignored</param>
        /// <returns>List of differences between the datasets</returns>
        [Obsolete("Throws exceptions in a lot of cases")]
        public static IEnumerable<string> Compare(DicomDataset a, DicomDataset b, bool ignoreTrailingNull = false)
        {
            if (a == null || b == null)
                throw new ArgumentException("Dataset " + (a == null ? "A" : "B") + " was null");

            var differences = new List<string>();

            if (!a.Any())
            {
                if (!b.Any())
                    return differences;

                differences.Add("A contained no elements, but B did");
                return differences;
            }

            if (!b.Any())
            {
                differences.Add("B contained no elements, but A did");
                return differences;
            }

            if (a.Count() != b.Count())
                differences.Add("A and B did not contain the same number of elements");

            foreach (DicomItem item in a)
            {
                if (!b.Contains(item.Tag))
                {
                    differences.Add($"B did not contain tag {item.Tag} {item.Tag.DictionaryEntry.Keyword} from A");
                    continue;
                }

                if (item.ValueRepresentation.IsString)
                {
                    string before = a.GetString(item.Tag);
                    string after = b.GetString(item.Tag);

                    if (string.Equals(before, after)) continue;

                    if (ignoreTrailingNull && Math.Abs(before.Length - after.Length) == 1)
                    {
                        string longest = before.Length > after.Length ? before : after;

                        // Check for a single trailing NUL character (int value == 0)
                        if (longest[longest.Length - 1] == 0)
                            continue;
                    }

                    differences.Add(string.Format("Tag {0} {1} {2} had value \"{3}\" in A and \"{4}\" in B",
                        item.Tag, item.ValueRepresentation, item.Tag.DictionaryEntry.Keyword, before, after));
                }
                else if (item.ValueRepresentation == DicomVR.SQ)
                {
                    DicomSequence seqA = a.GetSequence(item.Tag);
                    DicomSequence seqB = b.GetSequence(item.Tag);

                    if (seqA.Count() != seqB.Count())
                    {
                        differences.Add(string.Format("Sequence of tag {0} {1} had {2} elements in A, but {3} in B",
                            item.Tag, item.Tag.DictionaryEntry.Keyword, seqA.Count(), seqB.Count()));
                        continue;
                    }

                    for (var i = 0; i < seqA.Count(); ++i)
                        differences.AddRange(Compare(seqA.Items[i], seqB.Items[i]));
                }
                else
                {
                    object[] valA = a.GetValues<object>(item.Tag);
                    object[] valB = b.GetValues<object>(item.Tag);

                    if (!(valA.Any() || valB.Any()))
                        continue;

                    if (valA.Length != valB.Length)
                        differences.Add(string.Format("Tag {0} {1} {2} had {3} values in A and {4} values in B",
                            item.Tag, item.ValueRepresentation, item.Tag.DictionaryEntry.Keyword, valA.Length, valB.Length));

                    List<object> diffs = valA.Except(valB).ToList();

                    if (!diffs.Any())
                        continue;

                    differences.Add("\tDifferent values were: " + string.Join(", ", diffs));
                }
            }

            return differences;
        }

        public static string PrettyPrintBsonDocument(BsonDocument document)
        {
            var sb = new StringBuilder();

            sb.AppendLine("\n{");

            foreach (BsonElement item in document)
            {
                sb.Append("" + item.Name);

                if (item.Value is BsonDocument)
                    sb.AppendLine(PrettyPrintBsonDocument((BsonDocument)item.Value));
                else if (item.Value is BsonArray)
                    sb.AppendLine(":\t" + item.Value);
                else
                {
                    object value;

                    if (item.Value.IsBsonNull)
                        value = null;
                    else
                        value = item.Value;

                    sb.AppendLine(":\t\"" + value + "\"");
                }
            }

            sb.Append("}");

            return sb.ToString();
        }

        public static JsonConverter GetConverter(Type converterType)
        {
            const BindingFlags flags = BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance | BindingFlags.OptionalParamBinding;
            return (JsonConverter)Activator.CreateInstance(converterType, flags, null, new[] { Type.Missing }, null);
        }
    }
}
