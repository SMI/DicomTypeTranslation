
using Dicom;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
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
