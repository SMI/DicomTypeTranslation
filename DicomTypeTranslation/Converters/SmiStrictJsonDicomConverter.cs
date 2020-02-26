
// Copyright (c) 2012-2017 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom;
using Dicom.IO.Buffer;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DicomTypeTranslation.Converters
{
    /// <summary>
    /// Converts a DicomDataset object to and from JSON using the NewtonSoft Json.NET library
    /// COPIED VERSION - Original is: https://github.com/fo-dicom/fo-dicom/blob/development/Serialization/Json/JsonDicomConverter.cs
    /// </summary>
    [Obsolete("Will be removed")]
    public sealed class SmiStrictJsonDicomConverter : JsonConverter
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();


        static SmiStrictJsonDicomConverter()
        {
            DicomValidation.AutoValidation = false;
        }

        /// <summary>
        /// Constructor with 1 bool argument required for compatibility testing with
        /// original version of the class.
        /// </summary>
        /// <param name="unused"></param>
        // ReSharper disable once UnusedParameter.Local
        public SmiStrictJsonDicomConverter(bool unused = false) { }

        #region JsonConverter overrides

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var dataset = (DicomDataset)value;

            writer.WriteStartObject();

            foreach (DicomItem item in dataset)
            {
                // Group length (gggg,0000) attributes shall not be included in a DICOM JSON Model object.
                if (((uint)item.Tag & 0xffff) == 0)
                    continue;

                writer.WritePropertyName(item.Tag.Group.ToString("X4") + item.Tag.Element.ToString("X4"));
                WriteJsonDicomItem(writer, item, serializer);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dataset = new DicomDataset();

            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            while (reader.TokenType == JsonToken.PropertyName)
            {
                DicomTag tag = ParseTag((string)reader.Value);

                reader.Read();

                DicomItem item = ReadJsonDicomItem(tag, reader, serializer);

                dataset.Add(item);
                reader.Read();
            }

            // Ensure all private tags have a reference to their Private Creator tag
            foreach (DicomItem item in dataset)
            {
                if (item.Tag.IsPrivate && ((item.Tag.Element & 0xff00) != 0))
                {
                    var privateCreatorTag = new DicomTag(item.Tag.Group, (ushort)(item.Tag.Element >> 8));

                    if (dataset.Contains(privateCreatorTag))
                        item.Tag.PrivateCreator = new DicomPrivateCreator(dataset.GetValue<string>(privateCreatorTag, 0));
                }
            }

            if (reader.TokenType != JsonToken.EndObject)
                throw new JsonReaderException("Malformed DICOM json");

            return dataset;
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(DicomDataset).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        #endregion

        /// <summary>
        /// Create an instance of a IBulkDataUriByteBuffer. Override this method to use a different IBulkDataUriByteBuffer implementation in applications.
        /// </summary>
        /// <param name="bulkDataUri">The URI of a bulk data element as defined in <see cref="!:http://dicom.nema.org/medical/dicom/current/output/chtml/part19/chapter_A.html#table_A.1.5-2">Table A.1.5-2 in PS3.19</see>.</param>
        /// <returns>An instance of a Bulk URI Byte buffer.</returns>
        private IBulkDataUriByteBuffer CreateBulkDataUriByteBuffer(string bulkDataUri)
        {
            return new BulkDataUriByteBuffer(bulkDataUri);
        }

        #region Utilities

        private static DicomTag ParseTag(string tagStr)
        {
            ushort group = Convert.ToUInt16(tagStr.Substring(0, 4), 16);
            ushort element = Convert.ToUInt16(tagStr.Substring(4), 16);
            return new DicomTag(group, element);
        }

        private static DicomItem CreateDicomItem(DicomTag tag, string vr, object data)
        {
            DicomItem item;
            switch (vr)
            {
                case "AE":
                    item = new DicomApplicationEntity(tag, (string[])data);
                    break;
                case "AS":
                    item = new DicomAgeString(tag, (string[])data);
                    break;
                case "AT":
                    item = new DicomAttributeTag(tag, ((string[])data).Select(ParseTag).ToArray());
                    break;
                case "CS":
                    item = new DicomCodeString(tag, (string[])data);
                    break;
                case "DA":
                    item = new DicomDate(tag, (string[])data);
                    break;
                case "DS":
                    item = new DicomDecimalString(tag, (string[])data);
                    break;
                case "DT":
                    item = new DicomDateTime(tag, (string[])data);
                    break;
                case "FD":
                    item = new DicomFloatingPointDouble(tag, (double[])data);
                    break;
                case "FL":
                    item = new DicomFloatingPointSingle(tag, (float[])data);
                    break;
                case "IS":
                    item = new DicomIntegerString(tag, (int[])data);
                    break;
                case "LO":
                    item = new DicomLongString(tag, (string[])data);
                    break;
                case "LT":
                    item = new DicomLongText(tag, ((string[])data).Single());
                    break;
                case "OB":
                    item = new DicomOtherByte(tag, (IByteBuffer)data);
                    break;
                case "OD":
                    item = new DicomOtherDouble(tag, (IByteBuffer)data);
                    break;
                case "OF":
                    item = new DicomOtherFloat(tag, (IByteBuffer)data);
                    break;
                case "OL":
                    item = new DicomOtherLong(tag, (IByteBuffer)data);
                    break;
                case "OW":
                    item = new DicomOtherWord(tag, (IByteBuffer)data);
                    break;
                case "PN":
                    item = new DicomPersonName(tag, (string[])data);
                    break;
                case "SH":
                    item = new DicomShortString(tag, (string[])data);
                    break;
                case "SL":
                    item = new DicomSignedLong(tag, (int[])data);
                    break;
                case "SS":
                    item = new DicomSignedShort(tag, (short[])data);
                    break;
                case "ST":
                    item = new DicomShortText(tag, ((string[])data)[0]);
                    break;
                case "SQ":
                    item = new DicomSequence(tag, ((DicomDataset[])data));
                    break;
                case "TM":
                    item = new DicomTime(tag, (string[])data);
                    break;
                case "UC":
                    item = new DicomUnlimitedCharacters(tag, (string[])data);
                    break;
                case "UI":
                    item = new DicomUniqueIdentifier(tag, (string[])data);
                    break;
                case "UL":
                    item = new DicomUnsignedLong(tag, (uint[])data);
                    break;
                case "UN":
                    item = new DicomUnknown(tag, (IByteBuffer)data);
                    break;
                case "UR":
                    item = new DicomUniversalResource(tag, ((string[])data).Single());
                    break;
                case "US":
                    item = new DicomUnsignedShort(tag, (ushort[])data);
                    break;
                case "UT":
                    item = new DicomUnlimitedText(tag, ((string[])data).Single());
                    break;
                default:
                    throw new NotSupportedException("Unsupported value representation");
            }

            return item;
        }

        #endregion

        #region WriteJson helpers

        private void WriteJsonDicomItem(JsonWriter writer, DicomItem item, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("vr");
            writer.WriteValue(item.ValueRepresentation.Code);

            // Maybe do some validation here when it gets implemented?
            // Until then need to somehow try/catch the default write methods and write other data if not

            switch (item.ValueRepresentation.Code)
            {
                case "PN":
                    WriteJsonPersonName(writer, (DicomPersonName)item);
                    break;
                case "SQ":
                    WriteJsonSequence(writer, (DicomSequence)item, serializer);
                    break;
                case "OB":
                case "OD":
                case "OF":
                case "OL":
                case "OW":
                case "UN":
                    WriteJsonOther(writer, (DicomElement)item);
                    break;
                case "FL":
                    WriteJsonElement<float>(writer, (DicomElement)item);
                    break;
                case "FD":
                    WriteJsonElement<double>(writer, (DicomElement)item);
                    break;
                case "IS":
                    WriteJsonIntegerString(writer, (DicomElement)item);
                    break;
                case "SL":
                    WriteJsonElement<int>(writer, (DicomElement)item);
                    break;
                case "SS":
                    WriteJsonElement<short>(writer, (DicomElement)item);
                    break;
                case "UL":
                    WriteJsonElement<uint>(writer, (DicomElement)item);
                    break;
                case "US":
                    WriteJsonElement<ushort>(writer, (DicomElement)item);
                    break;
                case "DS":
                    // This is to conform to the Dicom specification for json... could always just parse this into a string though!
                    WriteJsonDecimalString(writer, (DicomElement)item);
                    break;
                case "AT":
                    WriteJsonAttributeTag(writer, (DicomElement)item);
                    break;
                default:
                    WriteJsonElement<string>(writer, (DicomElement)item);
                    break;
            }

            writer.WriteEndObject();
        }

        private static void WriteJsonIntegerString(JsonWriter writer, DicomElement elem)
        {
            if (elem.Count == 0)
                return;

            writer.WritePropertyName("Value");
            writer.WriteStartArray();

            foreach (string val in elem.Get<string[]>())
            {
                if (val == null || val.Equals(""))
                {
                    writer.WriteNull();
                }
                else
                {
                    // Want to try to fix the value and write as an int, otherwise have to throw since we can't read it back currently
                    string fix = FixIntegerString(val);

                    if (fix == null)
                    {
                        _logger.Warn("Could not parse IS value \"" + val + "\" to an integer, writing null and continuing");
                        writer.WriteNull();

                        continue;
                    }


                    if (int.TryParse(fix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                        writer.WriteValue(parsed);
                    else
                        throw new FormatException(string.Format("Cannot write dicom IntegerString \"{0}\" to json", val));
                }
            }

            writer.WriteEndArray();
        }

        private static string FixIntegerString(string val)
        {
            // Fix for null-terminated strings
            val = val.Replace("\0", " ");

            // All we can do is strip any leading 0s or spaces

            if (string.IsNullOrWhiteSpace(val))
                return null;

            val = val.Trim();

            var negative = false;

            // Strip leading superfluous plus signs
            if (val[0] == '+')
                val = val.Substring(1);

            else if (val[0] == '-')
            {
                // Temporarily remove negation sign for zero-stripping later
                negative = true;
                val = val.Substring(1);
            }

            // Strip leading superfluous zeros
            if (val.Length > 1 && val[0] == '0')
            {
                var i = 0;
                while (i < val.Length - 1 && val[i] == '0') i++;
                val = val.Substring(i);
            }

            // Re-add negation sign
            if (negative)
                val = "-" + val;

            return val;
        }

        private static void WriteJsonDecimalString(JsonWriter writer, DicomElement elem)
        {
            if (elem.Count == 0)
                return;

            writer.WritePropertyName("Value");
            writer.WriteStartArray();

            foreach (string val in elem.Get<string[]>())
            {
                if (val == null || val.Equals(""))
                {
                    writer.WriteNull();
                }
                else
                {
                    string fix = FixDecimalString(val);

                    if (fix == null)
                        throw new FormatException($"Could not parse DS value {val} to a valid json number");

                    if (ulong.TryParse(fix, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong xulong))
                        writer.WriteValue(xulong);
                    else if (long.TryParse(fix, NumberStyles.Integer, CultureInfo.InvariantCulture, out long xlong))
                        writer.WriteValue(xlong);
                    else if (decimal.TryParse(fix, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal xdecimal))
                        writer.WriteValue(xdecimal);
                    else if (double.TryParse(fix, NumberStyles.Float, CultureInfo.InvariantCulture, out double xdouble))
                        writer.WriteValue(xdouble);
                    else
                        throw new FormatException($"Could not parse DS value {fix} to a valid C# type");
                }
            }

            writer.WriteEndArray();
        }

        private static bool IsValidJsonNumber(string val)
        {
            // This is not very efficient - uses .NET regex caching
            return Regex.IsMatch(val, "^-?(0|[1-9][0-9]*)([.][0-9]+)?([eE][-+]?[0-9]+)?$");
        }

        /// <summary>
        /// Fix-up a Dicom DS number for use with json.
        /// Rationale: There is a requirement that DS numbers shall be written as json numbers in part 18.F json, but the
        /// requirements on DS allows values that are not json numbers. This method "fixes" them to conform to json numbers.
        /// </summary>
        /// <param name="val">A valid DS value</param>
        /// <returns>A json number equivalent to the supplied DS value</returns>
        private static string FixDecimalString(string val)
        {
            if (IsValidJsonNumber(val))
                return val;

            if (string.IsNullOrWhiteSpace(val))
                return null;

            // Fix for null-terminated strings
            val = val.Replace("\0", " ");

            // Fix for "-.123" etc.
            if (val.Length > 1 && val.StartsWith("-."))
                val = val.Insert(1, "0");

            // Fix for strings with leading "."
            if (val[0] == '.')
                val = val.Insert(0, "0");

            val = val.Trim();

            var negative = false;

            // Strip leading superfluous plus signs
            if (val[0] == '+')
            {
                val = val.Substring(1);
            }
            else if (val[0] == '-')
            {
                // Temporarily remove negation sign for zero-stripping later
                negative = true;
                val = val.Substring(1);
            }

            // Strip leading superfluous zeros
            if (val.Length > 1 && val[0] == '0' && val[1] != '.')
            {
                var i = 0;
                while (i < val.Length - 1 && val[i] == '0' && val[i + 1] != '.') i++;
                val = val.Substring(i);
            }

            // Re-add negation sign
            if (negative) val = "-" + val;

            // Fix for strings with trailing "."
            if (val.EndsWith("."))
                val += "0";

            return IsValidJsonNumber(val) ? val : null;
        }

        private static void WriteJsonElement<T>(JsonWriter writer, DicomElement elem)
        {
            if (elem.Count == 0)
                return;

            writer.WritePropertyName("Value");
            writer.WriteStartArray();

            foreach (T val in elem.Get<T[]>())
            {
                if (val == null || (typeof(T) == typeof(string) && val.Equals("")))
                    writer.WriteNull();
                else
                    writer.WriteValue(val);
            }

            writer.WriteEndArray();
        }

        private static void WriteJsonAttributeTag(JsonWriter writer, DicomElement elem)
        {
            if (elem.Count == 0)
                return;

            writer.WritePropertyName("Value");
            writer.WriteStartArray();

            foreach (DicomTag val in elem.Get<DicomTag[]>())
            {
                if (val == null)
                    writer.WriteNull();
                else
                    writer.WriteValue(((uint)val).ToString("X8"));
            }

            writer.WriteEndArray();
        }

        private static void WriteJsonOther(JsonWriter writer, DicomElement elem)
        {
            if (elem.Buffer is IBulkDataUriByteBuffer buffer)
            {
                writer.WritePropertyName("BulkDataURI");
                writer.WriteValue(buffer.BulkDataUri);
            }
            else if (elem.Count != 0)
            {
                writer.WritePropertyName("InlineBinary");
                writer.WriteValue(Convert.ToBase64String(elem.Buffer.Data));
            }
        }

        private void WriteJsonSequence(JsonWriter writer, DicomSequence seq, JsonSerializer serializer)
        {
            if (seq.Items.Count == 0)
                return;

            writer.WritePropertyName("Value");
            writer.WriteStartArray();

            foreach (DicomDataset child in seq.Items)
                WriteJson(writer, child, serializer);

            writer.WriteEndArray();
        }

        private static void WriteJsonPersonName(JsonWriter writer, DicomPersonName pn)
        {
            if (pn.Count == 0)
                return;

            writer.WritePropertyName("Value");
            writer.WriteStartArray();

            foreach (string val in pn.Get<string[]>())
            {
                if (string.IsNullOrEmpty(val))
                {
                    writer.WriteNull();
                }
                else
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("Alphabetic");
                    writer.WriteValue(val);
                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }

        #endregion

        #region ReadJson helpers

        private DicomItem ReadJsonDicomItem(DicomTag tag, JsonReader reader, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            if (reader.TokenType != JsonToken.PropertyName)
                throw new JsonReaderException("Malformed DICOM json");

            if ((string)reader.Value != "vr")
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            if (reader.TokenType != JsonToken.String)
                throw new JsonReaderException("Malformed DICOM json");

            var vr = (string)reader.Value;

            if (vr.Length != 2)
                throw new JsonReaderException("Malformed DICOM json");

            object data;

            switch (vr)
            {
                case "OB":
                case "OD":
                case "OF":
                case "OL":
                case "OW":
                case "UN":
                    data = ReadJsonOX(reader);
                    break;
                case "SQ":
                    data = ReadJsonSequence(reader, serializer);
                    break;
                case "PN":
                    data = ReadJsonPersonName(reader);
                    break;
                case "FL":
                    data = ReadJsonMultiNumber<float>(reader);
                    break;
                case "FD":
                    data = ReadJsonMultiNumber<double>(reader);
                    break;
                case "IS":
                case "SL":
                    data = ReadJsonMultiNumber<int>(reader);
                    break;
                case "SS":
                    data = ReadJsonMultiNumber<short>(reader);
                    break;
                case "UL":
                    data = ReadJsonMultiNumber<uint>(reader);
                    break;
                case "US":
                    data = ReadJsonMultiNumber<ushort>(reader);
                    break;
                case "DS":
                    data = ReadJsonMultiString(reader);
                    break;
                default:
                    data = ReadJsonMultiString(reader);
                    break;
            }

            if (reader.TokenType != JsonToken.EndObject)
                throw new JsonReaderException("Malformed DICOM json");

            DicomItem item = CreateDicomItem(tag, vr, data);

            return item;
        }

        private object ReadJsonMultiString(JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "Value")
            {
                return ReadJsonMultiStringValue(reader);
            }

            if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "BulkDataURI")
            {
                return ReadJsonBulkDataUri(reader);
            }

            return new string[0];
        }

        private static string[] ReadJsonMultiStringValue(JsonReader reader)
        {
            var childStrings = new List<string>();

            reader.Read();

            if (reader.TokenType == JsonToken.EndObject)
                return new string[0];

            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonReaderException("Malformed DICOM json");

            reader.ReadAsString();

            while (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Null)
            {
                if (reader.TokenType == JsonToken.Null)
                    childStrings.Add(null);
                else
                    childStrings.Add((string)reader.Value);

                reader.ReadAsString();
            }

            if (reader.TokenType != JsonToken.EndArray)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            return childStrings.ToArray();
        }

        private static T[] ReadJsonMultiNumber<T>(JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType == JsonToken.EndObject)
                return new T[0];

            if (!(reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "Value"))
                throw new JsonReaderException("Malformed DICOM json");

            var childValues = new List<T>();

            reader.Read();

            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            while (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
            {
                childValues.Add((T)Convert.ChangeType(reader.Value, typeof(T)));
                reader.Read();
            }

            if (reader.TokenType != JsonToken.EndArray)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            return childValues.ToArray();
        }

        private static string[] ReadJsonPersonName(JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType != JsonToken.PropertyName || (string)reader.Value != "Value")
                return new string[0];

            var childStrings = new List<string>();

            reader.Read();

            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            while (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.Null)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    childStrings.Add(null);
                }
                else
                {
                    reader.Read();

                    if (reader.TokenType != JsonToken.PropertyName)
                        throw new JsonReaderException("Malformed DICOM json");

                    if ((string)reader.Value == "Alphabetic")
                    {
                        reader.Read();

                        if (reader.TokenType != JsonToken.String)
                            throw new JsonReaderException("Malformed DICOM json");

                        childStrings.Add((string)reader.Value);
                    }
                    else
                    {
                        reader.Read();
                    }

                    reader.Read();

                    if (reader.TokenType != JsonToken.EndObject)
                        throw new JsonReaderException("Malformed DICOM json");
                }

                reader.Read();
            }

            if (reader.TokenType != JsonToken.EndArray)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            return childStrings.ToArray();

        }

        private DicomDataset[] ReadJsonSequence(JsonReader reader, JsonSerializer serializer)
        {
            reader.Read();

            if (reader.TokenType != JsonToken.PropertyName || (string)reader.Value != "Value")
                return new DicomDataset[0];

            reader.Read();

            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            var childItems = new List<DicomDataset>();

            while (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.Null)
            {
                childItems.Add((DicomDataset)ReadJson(reader, typeof(DicomDataset), null, serializer));
                reader.Read();
            }

            if (reader.TokenType != JsonToken.EndArray)
                throw new JsonReaderException("Malformed DICOM json");

            reader.Read();

            return childItems.ToArray();
        }

        private IByteBuffer ReadJsonOX(JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "InlineBinary")
            {
                return ReadJsonInlineBinary(reader);
            }

            if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "BulkDataURI")
            {
                return ReadJsonBulkDataUri(reader);
            }

            return EmptyBuffer.Value;
        }

        private static IByteBuffer ReadJsonInlineBinary(JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType != JsonToken.String)
                throw new JsonReaderException("Malformed DICOM json");

            var data = new MemoryByteBuffer(Convert.FromBase64String(reader.Value as string));
            reader.Read();

            return data;
        }

        private IBulkDataUriByteBuffer ReadJsonBulkDataUri(JsonReader reader)
        {
            reader.Read();

            if (reader.TokenType != JsonToken.String)
                throw new JsonReaderException("Malformed DICOM json");

            IBulkDataUriByteBuffer data = CreateBulkDataUriByteBuffer((string)reader.Value);
            reader.Read();

            return data;
        }

        #endregion
    }
}
