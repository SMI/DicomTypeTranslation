
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;

namespace DicomTypeTranslation.Converters;

/// <summary>
/// SMI JSON to DICOM converter. Lazily converts between certain DICOM value types to allow greater coverage over real data.
/// This means it does not comply with the formal DICOM JSON specification.
/// </summary>
public class SmiJsonDicomConverter
{
    private const string VALUE_PROPERTY_NAME = "val";
    private const string INL_BIN_PROPERTY_NAME = "bin";
    private const string BLK_URI_PROPERTY_NAME = "uri";
    private const string VR_PROPERTY_NAME = "vr";

    /// <summary>
    /// Convert this dataset to a json string
    /// </summary>
    /// <param name="dataset"></param>
    /// <returns></returns>
    public static string ToJson(DicomDataset dataset)
    {
        var self=new SmiJsonDicomConverter();
        using var ms = new MemoryStream();
        JsonWriterOptions writerOptions=new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        using var writer = new Utf8JsonWriter(ms,writerOptions);
        self.WriteJson(writer, dataset);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    /// <summary>
    /// Convert a json string to a dataset
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static DicomDataset FromJson(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return ReadJson(ref reader);
    }

    /// <summary>
    /// Constructor with 1 boolean argument required for compatibility testing with
    /// original version of the class.
    /// </summary>
    /// <param name="_"></param>
    // ReSharper disable once UnusedParameter.Local
    public SmiJsonDicomConverter(bool _ = false) { }

    #region JsonConverter overrides

    private void WriteJson(Utf8JsonWriter writer, object value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var dataset = (DicomDataset)value;

        writer.WriteStartObject();

        foreach (var item in dataset)
        {
            // Group length (gggg,0000) attributes shall not be included in a DICOM JSON Model object.
            if (item.Tag.Element == 0)
                continue;

            writer.WritePropertyName($"{item.Tag.Group:X4}{item.Tag.Element:X4}");
            WriteJsonDicomItem(writer, item);
        }

        writer.WriteEndObject();
    }

    private static DicomDataset ReadJson(ref Utf8JsonReader reader)
    {
        var dataset = new DicomDataset();
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new Exception("Malformed DICOM json");

        reader.Read();

        while (reader.TokenType == JsonTokenType.PropertyName)
        {
            var tag = ParseTag(reader.GetString());

            reader.Read();

            var item = ReadJsonDicomItem(tag, ref reader);

            dataset.Add(item);

            reader.Read();
        }

        // Ensure all private tags have a reference to their Private Creator tag
        foreach (var item in dataset)
        {
            if (!item.Tag.IsPrivate || (item.Tag.Element & 0xff00) == 0) continue;

            var privateCreatorTag = new DicomTag(item.Tag.Group, (ushort)(item.Tag.Element >> 8));

            if (dataset.Contains(privateCreatorTag))
                item.Tag.PrivateCreator = new DicomPrivateCreator(dataset.GetSingleValue<string>(privateCreatorTag));
        }

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new Exception("Malformed DICOM json");

        return dataset;
    }

    #endregion

    #region WriteJson helpers

    private void WriteJsonDicomItem(Utf8JsonWriter writer, DicomItem item)
    {
        writer.WriteStartObject();
        writer.WriteString(VR_PROPERTY_NAME,item.ValueRepresentation.Code);

        // Could also do if(item is DicomStringElement) {...} here, but better to be explicit
        // in specifying each VR and also to match the old version of the converter
        switch (item.ValueRepresentation.Code)
        {
            // Subclasses of DicomStringElement

            case "AE": // Application Entity
            case "AS": // Age String
            case "CS": // Code String
            case "DS": // Decimal String
            case "IS": // Integer String
            case "LO": // Long String
            case "PN": // Person Name
            case "SH": // Short String
            case "UI": // Unique Identifier
            case "UC": // Unlimited Characters
            case "LT": // Long Text
            case "ST": // Short Text
            case "UR": // Universal Resource
            case "UT": // Unlimited Text
            case "DA": // Date
            case "DT": // DateTime
            case "TM": // Time
                WriteJsonStringElement(writer, (DicomElement)item);
                break;

            // Subclasses of DicomValueElement<T>

            // DicomValueElement<double>
            case "FD": // Floating Point Double
                WriteDicomValueElement<double>(writer, (DicomElement)item);
                break;

            // DicomValueElement<float>
            case "FL": // Floating Point Single
                WriteDicomValueElement<float>(writer, (DicomElement)item);
                break;

            // DicomValueElement<int>
            case "SL": // Signed Long
                WriteDicomValueElement<int>(writer, (DicomElement)item);
                break;

            // DicomValueElement<uint>
            case "UL": // Unsigned Long
                WriteDicomValueElement<uint>(writer, (DicomElement)item);
                break;

            // DicomValueElement<short>
            case "SS": // Signed Short
                WriteDicomValueElement<short>(writer, (DicomElement)item);
                break;

            case "SV": // Signed 64-bit Very Long
                WriteDicomValueElement<long>(writer, (DicomElement)item);
                break;

            // DicomValueElement<ushort>
            case "US": // Unsigned Short
                WriteDicomValueElement<ushort>(writer, (DicomElement)item);
                break;

            case "UV": // Unsigned 64-bit Very Long
                WriteDicomValueElement<ulong>(writer, (DicomElement)item);
                break;

            // Other element types

            case "AT": // Attribute Tag
                WriteJsonAttributeTag(writer, (DicomElement)item);
                break;

            case "SQ": // Sequence
                WriteJsonSequence(writer, (DicomSequence)item);
                break;

            case "OB":
            case "OD":
            case "OF":
            case "OL":
            case "OV":
            case "OW":
            case "UN":
                WriteJsonOther(writer, item);
                break;

            default:
                throw new ArgumentException(
                    $"No case implemented to write data for VR {item.ValueRepresentation.Code} to JSON");
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Write any subclass of <see cref="DicomStringElement"/> to json
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="elem"></param>
    private static void WriteJsonStringElement(Utf8JsonWriter writer, DicomElement elem)
    {
        if (elem.Count != 0) writer.WriteString(VALUE_PROPERTY_NAME, elem.Get<string>().TrimEnd('\0'));
    }

    private static void WriteDicomValueElement<T>(Utf8JsonWriter writer, DicomElement elem) where T : struct
    {
        if (elem.Count == 0)
            return;
        writer.WriteStartArray(VALUE_PROPERTY_NAME);

        foreach (var val in elem.Get<T[]>())
            switch (val)
            {
                case ulong u: writer.WriteNumberValue(u);
                    break;
                case uint u: writer.WriteNumberValue(u);
                    break;
                case ushort u: writer.WriteNumberValue(u);
                    break;
                case long u: writer.WriteNumberValue(u);
                    break;
                case int u: writer.WriteNumberValue(u);
                    break;
                case short u: writer.WriteNumberValue(u);
                    break;
                case float u: writer.WriteNumberValue(u);
                    break;
                case double u: writer.WriteNumberValue(u);
                    break;
                default:
                    throw new ArgumentException($"No case implemented to write data for type {typeof(T)} to JSON");
            }

        writer.WriteEndArray();
    }

    private static void WriteJsonAttributeTag(Utf8JsonWriter writer, DicomElement elem)
    {
        if (elem.Count == 0)
            return;

        writer.WritePropertyName(VALUE_PROPERTY_NAME);

        var sb = new StringBuilder();

        foreach (var val in elem.Get<DicomTag[]>())
            sb.Append(((uint)val).ToString("X8"));

        if (sb.Length % 8 != 0)
            throw new JsonException($"AttributeTag string of length {sb.Length} is not divisible by 8");

        writer.WriteStringValue(sb.ToString());
    }
    private void WriteJsonSequence(Utf8JsonWriter writer, DicomSequence seq)
    {
        if (seq.Items.Count == 0)
            return;

        writer.WritePropertyName(VALUE_PROPERTY_NAME);
        writer.WriteStartArray();

        foreach (var child in seq.Items)
            WriteJson(writer, child);

        writer.WriteEndArray();
    }

    private static void WriteJsonOther(Utf8JsonWriter writer, DicomItem item)
    {
        // DicomFragmentSequence - Only for pixel data
        if (item is not DicomElement elem)
            return;

        if (!DicomTypeTranslater.SerializeBinaryData && DicomTypeTranslater.DicomVrBlacklist.Contains(elem.ValueRepresentation))
            return;

        if (elem.Buffer is IBulkDataUriByteBuffer buffer)
        {
            writer.WritePropertyName(BLK_URI_PROPERTY_NAME);
            writer.WriteStringValue(buffer.BulkDataUri);
        }
        else if (elem.Count != 0)
        {
            writer.WritePropertyName(INL_BIN_PROPERTY_NAME);
            writer.WriteStringValue(Convert.ToBase64String(elem.Buffer.Data));
        }
    }

    #endregion

    #region ReadJson helpers

    private static DicomTag ParseTag(string tagStr)
    {
        var group = Convert.ToUInt16(tagStr[..4], 16);
        var element = Convert.ToUInt16(tagStr[4..], 16);
        var tag = new DicomTag(group, element);
        return tag;
    }

    private static DicomItem ReadJsonDicomItem(DicomTag tag, ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new Exception("Malformed DICOM json");

        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName)
            throw new Exception("Malformed DICOM json");

        if (reader.GetString() != VR_PROPERTY_NAME)
            throw new Exception("Malformed DICOM json");

        reader.Read();

        if (reader.TokenType != JsonTokenType.String)
            throw new Exception("Malformed DICOM json");

        var vr = reader.GetString();

        if (vr?.Length != 2)
            throw new Exception("Malformed DICOM json");

        object data = vr switch
        {
            // Subclasses of DicomStringElement
            "AE" => // Application Entity
                ReadJsonString(ref reader),
            "AS" => // Age String
                ReadJsonString(ref reader),
            "CS" => // Code String
                ReadJsonString(ref reader),
            "DS" => // Decimal String
                ReadJsonString(ref reader),
            "IS" => // Integer String
                ReadJsonString(ref reader),
            "LO" => // Long String
                ReadJsonString(ref reader),
            "PN" => // Person Name
                ReadJsonString(ref reader),
            "SH" => // Short String
                ReadJsonString(ref reader),
            "UI" => // Unique Identifier
                ReadJsonString(ref reader),
            "UC" => // Unlimited Characters
                ReadJsonString(ref reader),
            "LT" => // Long Text
                ReadJsonString(ref reader),
            "ST" => // Short Text
                ReadJsonString(ref reader),
            "UR" => // Universal Resource
                ReadJsonString(ref reader),
            "UT" => // Unlimited Text
                ReadJsonString(ref reader),
            "DA" => // Date
                ReadJsonString(ref reader),
            "DT" => // DateTime
                ReadJsonString(ref reader),
            "TM" => // Time
                ReadJsonString(ref reader),
            "AT" => // Attribute Tag
                ReadJsonString(ref reader),
            // Subclasses of DicomValueElement<T>
            // DicomValueElement<double>
            "FD" => // Floating Point Double
                ReadJsonNumeric<double>(ref reader),
            // DicomValueElement<float>
            "FL" => // Floating Point Single
                ReadJsonNumeric<float>(ref reader),
            // DicomValueElement<int>
            "SL" => // Signed Long
                ReadJsonNumeric<int>(ref reader),
            // DicomValueElement<uint>
            "UL" => // Unsigned Long
                ReadJsonNumeric<uint>(ref reader),
            // DicomValueElement<short>
            "SS" => // Signed Short
                ReadJsonNumeric<short>(ref reader),
            "SV" => ReadJsonNumeric<long>(ref reader),
            // DicomValueElement<ushort>
            "US" => // Unsigned Short
                ReadJsonNumeric<ushort>(ref reader),
            "UV" => ReadJsonNumeric<ulong>(ref reader),
            // Sequence
            "SQ" => ReadJsonSequence(ref reader),
            // OtherX elements
            "OB" => ReadJsonOX(ref reader),
            "OD" => ReadJsonOX(ref reader),
            "OF" => ReadJsonOX(ref reader),
            "OL" => ReadJsonOX(ref reader),
            "OW" => ReadJsonOX(ref reader),
            "OV" => ReadJsonOX(ref reader),
            "UN" => ReadJsonOX(ref reader),
            _ => throw new ArgumentException($"No case implemented to read object data for VR {vr} from JSON")
        };

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new Exception($"Malformed DICOM json, got {reader.TokenType} when expecting {nameof(JsonTokenType.EndObject)}");

        return CreateDicomItem(tag, vr, data);
    }

    private static DicomItem CreateDicomItem(DicomTag tag, string vr, object data)
    {
        return vr switch
        {
            "AE" => new DicomApplicationEntity(tag, (string)data),
            "AS" => new DicomAgeString(tag, (string)data),
            "AT" => CreateDicomAttributeTag(tag, (string)data),
            "CS" => new DicomCodeString(tag, (string)data),
            "DA" => new DicomDate(tag, (string)data),
            "DS" => new DicomDecimalString(tag, (string)data),
            "DT" => new DicomDateTime(tag, (string)data),
            "FD" => new DicomFloatingPointDouble(tag, (double[])data),
            "FL" => new DicomFloatingPointSingle(tag, (float[])data),
            "IS" => new DicomIntegerString(tag, (string)data),
            "LO" => new DicomLongString(tag, (string)data),
            "LT" => new DicomLongText(tag, (string)data),
            "OB" => new DicomOtherByte(tag, (IByteBuffer)data),
            "OD" => new DicomOtherDouble(tag, (IByteBuffer)data),
            "OF" => new DicomOtherFloat(tag, (IByteBuffer)data),
            "OL" => new DicomOtherLong(tag, (IByteBuffer)data),
            "OW" => new DicomOtherWord(tag, (IByteBuffer)data),
            "OV" => new DicomOtherVeryLong(tag, (IByteBuffer)data),
            "PN" => new DicomPersonName(tag, (string)data),
            "SH" => new DicomShortString(tag, (string)data),
            "SL" => new DicomSignedLong(tag, (int[])data),
            "SQ" => new DicomSequence(tag, (DicomDataset[])data),
            "SS" => new DicomSignedShort(tag, (short[])data),
            "ST" => new DicomShortText(tag, (string)data),
            "SV" => new DicomSignedVeryLong(tag, (long[])data),
            "TM" => new DicomTime(tag, (string)data),
            "UC" => new DicomUnlimitedCharacters(tag, (string)data),
            "UI" => new DicomUniqueIdentifier(tag, (string)data),
            "UL" => new DicomUnsignedLong(tag, (uint[])data),
            "UN" => new DicomUnknown(tag, (IByteBuffer)data),
            "UR" => new DicomUniversalResource(tag, (string)data),
            "US" => new DicomUnsignedShort(tag, (ushort[])data),
            "UT" => new DicomUnlimitedText(tag, (string)data),
            "UV" => new DicomUnsignedVeryLong(tag, (ulong[])data),
            _ => throw new ArgumentException($"No method implemented to create a DicomItem of VR {vr} from JSON")
        };
    }

    private static string ReadJsonString(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.EndObject)
            return "";

        if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != VALUE_PROPERTY_NAME)
            throw new Exception("Malformed DICOM json");

        reader.Read();
        string val = reader.GetString();
        reader.Read();
        return val;
    }

    private static DicomAttributeTag CreateDicomAttributeTag(DicomTag tag, string str)
    {
        if (str == "")
            return new DicomAttributeTag(tag);

        if (str.Length % 8 != 0)
            throw new JsonException(
                $"Can't parse string of length {str.Length} to an AttributeTag. (Needs to be divisible by 8)");

        var split = new List<string>();

        for (var i = 0; i < str.Length; i += 8)
            split.Add(str.Substring(i, 8));

        return new DicomAttributeTag(tag, split.Select(ParseTag).ToArray());
    }

    private static T[] ReadJsonNumeric<T>(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.EndObject)
            return Array.Empty<T>();

        if (reader.TokenType != JsonTokenType.PropertyName && reader.GetString() != VALUE_PROPERTY_NAME)
            throw new Exception("Malformed DICOM json");

        reader.Read();

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new Exception("Malformed DICOM json");

        reader.Read();

        var values = new List<T>();

        while (reader.TokenType is JsonTokenType.Number)
        {
            values.Add((T)Convert.ChangeType(reader.GetDouble(), typeof(T)));
            reader.Read();
        }

        if (reader.TokenType != JsonTokenType.EndArray)
            throw new Exception("Malformed DICOM json");
        reader.Read();
        return values.ToArray();
    }

    private static IByteBuffer ReadJsonOX(ref Utf8JsonReader reader)
    {
        reader.Read();

        return reader.TokenType switch
        {
            JsonTokenType.PropertyName when reader.GetString() == INL_BIN_PROPERTY_NAME => ReadJsonInlineBinary(ref reader),
            JsonTokenType.PropertyName when reader.GetString() == BLK_URI_PROPERTY_NAME =>
                ReadJsonBulkDataUri(ref reader),
            _ => EmptyBuffer.Value
        };
    }

    private static IByteBuffer ReadJsonInlineBinary(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType != JsonTokenType.String)
            throw new Exception("Malformed DICOM json");

        var data = new MemoryByteBuffer(reader.GetBytesFromBase64());
        reader.Read();

        return data;
    }

    private static IBulkDataUriByteBuffer ReadJsonBulkDataUri(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType != JsonTokenType.String)
            throw new Exception("Malformed DICOM json");

        var data = new BulkDataUriByteBuffer(reader.GetString());
        reader.Read();

        return data;
    }

    private static DicomDataset[] ReadJsonSequence(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != VALUE_PROPERTY_NAME)
            return Array.Empty<DicomDataset>();

        reader.Read();

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new Exception("Malformed DICOM json");

        reader.Read();

        var childItems = new List<DicomDataset>();

        while (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.Null)
        {
            childItems.Add(ReadJson(ref reader));
            reader.Read();
        }

        if (reader.TokenType != JsonTokenType.EndArray)
            throw new Exception("Malformed DICOM json");
        reader.Read();
        return childItems.ToArray();
    }

    #endregion
}