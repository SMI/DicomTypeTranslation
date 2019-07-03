
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dicom;
using JetBrains.Annotations;
using MongoDB.Bson;


namespace DicomTypeTranslation
{
    /// <summary>
    /// Helper class for rapidly writing <see cref="DicomTag"/> values into a <see cref="DicomDataset"/> in basic C# Types (string, int, double etc).  Also supports
    /// Bson types (for MongoDb).
    /// </summary>
    public static class DicomTypeTranslaterWriter
    {
        /// <summary>
        /// Methods to call to add the given Type to the dataset (requires casting due to generic T) and sometimes you have to call Add(a,b) sometimes only Add(b) works
        /// </summary>
        private static readonly Dictionary<Type, Action<DicomDataset, DicomTag, object>> _dicomAddMethodDictionary = new Dictionary<Type, Action<DicomDataset, DicomTag, object>>();

        private static readonly Regex _privateCreatorRegex = new Regex(@":(.*)\)-");

        private static readonly string[] _ignoredBsonKeys = { "_id", "header" };


        static DicomTypeTranslaterWriter()
        {
            // Single types
            _dicomAddMethodDictionary.Add(typeof(string), (ds, t, o) => ds.Add(t, (string)o));
            _dicomAddMethodDictionary.Add(typeof(DicomTag), (ds, t, o) => ds.Add(t, (DicomTag)o));
            _dicomAddMethodDictionary.Add(typeof(DateTime), (ds, t, o) => ds.Add(t, (DateTime)o));
            _dicomAddMethodDictionary.Add(typeof(decimal), (ds, t, o) => ds.Add(t, (decimal)o));
            _dicomAddMethodDictionary.Add(typeof(double), (ds, t, o) => ds.Add(t, (double)o));
            _dicomAddMethodDictionary.Add(typeof(float), (ds, t, o) => ds.Add(t, (float)o));
            _dicomAddMethodDictionary.Add(typeof(int), (ds, t, o) => ds.Add(t, (int)o));
            _dicomAddMethodDictionary.Add(typeof(byte), (ds, t, o) => ds.Add(t, (byte)o));
            _dicomAddMethodDictionary.Add(typeof(short), (ds, t, o) => ds.Add(t, (short)o));
            _dicomAddMethodDictionary.Add(typeof(ushort), (ds, t, o) => ds.Add(t, (ushort)o));
            _dicomAddMethodDictionary.Add(typeof(DicomDateRange), (ds, t, o) => ds.Add(t, (DicomDateRange)o));
            _dicomAddMethodDictionary.Add(typeof(DicomDataset), (ds, t, o) => ds.Add(t, (DicomDataset)o));
            _dicomAddMethodDictionary.Add(typeof(DicomUID), (ds, t, o) => ds.Add(t, (DicomUID)o));
            _dicomAddMethodDictionary.Add(typeof(DicomTransferSyntax), (ds, t, o) => ds.Add(t, (DicomTransferSyntax)o));
            _dicomAddMethodDictionary.Add(typeof(DicomSequence), (ds, t, o) => ds.Add((DicomSequence)o));
            _dicomAddMethodDictionary.Add(typeof(uint), (ds, t, o) => ds.Add(t, (uint)o));
            _dicomAddMethodDictionary.Add(typeof(long), (ds, t, o) => ds.Add(t, (long)o));

            // Array types
            _dicomAddMethodDictionary.Add(typeof(string[]), (ds, t, o) => ds.Add(t, (string[])o));
            _dicomAddMethodDictionary.Add(typeof(DicomTag[]), (ds, t, o) => ds.Add(t, (DicomTag[])o));
            _dicomAddMethodDictionary.Add(typeof(DateTime[]), (ds, t, o) => ds.Add(t, (DateTime[])o));
            _dicomAddMethodDictionary.Add(typeof(decimal[]), (ds, t, o) => ds.Add(t, (decimal[])o));
            _dicomAddMethodDictionary.Add(typeof(double[]), (ds, t, o) => ds.Add(t, (double[])o));
            _dicomAddMethodDictionary.Add(typeof(float[]), (ds, t, o) => ds.Add(t, (float[])o));
            _dicomAddMethodDictionary.Add(typeof(int[]), (ds, t, o) => ds.Add(t, (int[])o));
            _dicomAddMethodDictionary.Add(typeof(byte[]), (ds, t, o) => ds.Add(t, (byte[])o));
            _dicomAddMethodDictionary.Add(typeof(short[]), (ds, t, o) => ds.Add(t, (short[])o));
            _dicomAddMethodDictionary.Add(typeof(ushort[]), (ds, t, o) => ds.Add(t, (ushort[])o));
            _dicomAddMethodDictionary.Add(typeof(DicomDateRange[]), (ds, t, o) => ds.Add(t, (DicomDateRange[])o));
            _dicomAddMethodDictionary.Add(typeof(DicomDataset[]), (ds, t, o) => ds.Add(t, (DicomDataset[])o));
            _dicomAddMethodDictionary.Add(typeof(DicomUID[]), (ds, t, o) => ds.Add(t, (DicomUID[])o));
            _dicomAddMethodDictionary.Add(typeof(DicomTransferSyntax[]), (ds, t, o) => ds.Add(t, (DicomTransferSyntax[])o));
            _dicomAddMethodDictionary.Add(typeof(DicomSequence[]), (ds, t, o) => ds.Add((DicomSequence[])o));
            _dicomAddMethodDictionary.Add(typeof(uint[]), (ds, t, o) => ds.Add(t, (uint[])o));
            _dicomAddMethodDictionary.Add(typeof(long[]), (ds, t, o) => ds.Add(t, (long[])o));

            //Those Involving something more complicated than simply forcing the generic <T> by casting something already of that Type
            _dicomAddMethodDictionary.Add(typeof(TimeSpan), (ds, t, o) => ds.Add(t, TimeSpanToDate((TimeSpan)o)));
            _dicomAddMethodDictionary.Add(typeof(TimeSpan[]), (ds, t, o) => ds.Add(t, ((TimeSpan[])o).Select(TimeSpanToDate).ToArray()));
        }

        private static DateTime TimeSpanToDate(TimeSpan ts)
        {
            return new DateTime(1, 1, 1, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
        }

        /// <inheritdoc cref="SetDicomTag(DicomDataset, DicomTag, object)"/>
        [UsedImplicitly]
        public static void SetDicomTag(DicomDataset dataset, DicomDictionaryEntry tag, object value)
        {
            SetDicomTag(dataset, tag.Tag, value);
        }

        /// <summary>
        /// Sets the given <paramref name="tag"/> in the <paramref name="dataset"/> to <paramref name="value"/>.  If <paramref name="value"/> is an <see cref="Array"/> 
        /// then the tag will be given multiplicity.  If the <paramref name="value"/> is an <see cref="IDictionary{TKey, TValue}"/> a <see cref="DicomSequence"/> will
        /// be created.
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="tag"></param>
        /// <param name="value"></param>
        public static void SetDicomTag(DicomDataset dataset, DicomTag tag, object value)
        {
            if (value == null)
            {
                // Need to specify a type for the generic, even though it is ignored
                dataset.Add<string>(tag);
                return;
            }

            //input is a dictionary of DicomTag=>Objects then it is a Sequence
            if (value is Dictionary<DicomTag, object>[] sequenceArray)
            {
                SetSequenceFromObject(dataset, tag, sequenceArray);
                return;
            }

            // Otherwise do generic add
            Type key;
            if (_dicomAddMethodDictionary.ContainsKey(value.GetType()))
                key = value.GetType();
            else
                key = _dicomAddMethodDictionary.Keys.FirstOrDefault(k => k.IsInstanceOfType(value));

            if (key == null)
                throw new Exception("No method to call for value type " + value.GetType());

            _dicomAddMethodDictionary[key](dataset, tag, value);
        }

        private static void SetSequenceFromObject(DicomDataset parentDataset, DicomTag tag, Dictionary<DicomTag, object>[] sequenceArray)
        {
            var sequenceList = new List<Dictionary<DicomTag, object>>(sequenceArray);

            var subDatasets = new List<DicomDataset>();

            foreach (Dictionary<DicomTag, object> sequenceDict in sequenceList)
            {
                var subDataset = new DicomDataset();

                foreach (KeyValuePair<DicomTag, object> kvp in sequenceDict)
                    SetDicomTag(subDataset, kvp.Key, kvp.Value);

                subDatasets.Add(subDataset);
            }

            parentDataset.Add(new DicomSequence(tag, subDatasets.ToArray()));
        }

        #region Bson Types

        /// <summary>
        /// Converts the <paramref name="document"/> into a <see cref="DicomDataset"/>
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static DicomDataset BuildDicomDataset(BsonDocument document)
        {
            var dataset = new DicomDataset();

            foreach (BsonElement element in document)
            {
                if (_ignoredBsonKeys.Contains(element.Name))
                    continue;

                if (element.Name.Contains("PrivateCreator"))
                {
                    DicomTag creatorTag = DicomTag.Parse(element.Name);
                    dataset.Add(new DicomLongString(new DicomTag(creatorTag.Group, creatorTag.Element), element.Value["val"].AsString));
                    continue;
                }

                DicomTag tag = TryParseTag(dataset, element);
                DicomVR vr = TryParseVr(element.Value);
                if (vr == null)
                    dataset.Add(CreateDicomItem(tag, element.Value));
                else
                    dataset.Add(CreateDicomItem(tag, element.Value["val"], vr));
            }

            return dataset;
        }

        private static DicomTag TryParseTag(DicomDataset dataset, BsonElement element)
        {
            string tagString = element.Name;

            try
            {
                DicomTag tag = tagString.StartsWith("(")
                    ? DicomTag.Parse(tagString)
                    : DicomDictionary.Default[tagString];

                if (!tag.IsPrivate)
                    return tag;

                string creatorName = _privateCreatorRegex.Match(element.Name).Groups[1].Value;
                tag = dataset.GetPrivateTag(new DicomTag(tag.Group, tag.Element, DicomDictionary.Default.GetPrivateCreator(creatorName)));

                return tag;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Could not parse tag from string: " + tagString, e);
            }
        }

        private static DicomVR TryParseVr(BsonValue bsonValue)
        {
            if (bsonValue == BsonNull.Value)
                return null;

            var asBsonDocument = bsonValue as BsonDocument;
            if (asBsonDocument == null || !asBsonDocument.Contains("vr"))
                return null;

            return DicomVR.Parse(asBsonDocument["vr"].AsString);
        }

        private static DicomItem CreateDicomItem(DicomTag tag, BsonValue data, DicomVR vr = null)
        {
            // Ok to throw an exception here - we should always be writing the VR into the Bson document if it's ambiguous
            if (vr == null)
                vr = tag.DictionaryEntry.ValueRepresentations.Single();

            DicomItem item;

            switch (vr.Code)
            {
                case "AE":
                    item = new DicomApplicationEntity(tag, GetString(data));
                    break;
                case "AS":
                    item = new DicomAgeString(tag, GetString(data));
                    break;
                case "AT":
                    item = ParseAttributeTag(tag, data);
                    break;
                case "CS":
                    item = new DicomCodeString(tag, GetString(data));
                    break;
                case "DA":
                    item = new DicomDate(tag, GetString(data));
                    break;
                case "DS":
                    item = new DicomDecimalString(tag, GetString(data));
                    break;
                case "DT":
                    item = new DicomDateTime(tag, GetString(data));
                    break;
                case "FD":
                    item = new DicomFloatingPointDouble(tag, (double[])GetTypedArray<double>(data));
                    break;
                case "FL":
                    item = new DicomFloatingPointSingle(tag, (float[])GetTypedArray<float>(data));
                    break;
                case "IS":
                    item = new DicomIntegerString(tag, GetString(data));
                    break;
                case "LO":
                    item = new DicomLongString(tag, Encoding.UTF8, GetString(data));
                    break;
                case "LT":
                    item = new DicomLongText(tag, Encoding.UTF8, GetString(data));
                    break;
                case "OB":
                    item = data.IsBsonNull
                        ? new DicomOtherByte(tag)
                        : new DicomOtherByte(tag, data.AsByteArray);
                    break;
                case "OD":
                    item = new DicomOtherDouble(tag, (double[])GetTypedArray<double>(data));
                    break;
                case "OF":
                    item = new DicomOtherFloat(tag, (float[])GetTypedArray<float>(data));
                    break;
                case "OL":
                    item = new DicomOtherLong(tag, (uint[])GetTypedArray<uint>(data));
                    break;
                case "OW":
                    item = data.IsBsonNull
                        ? new DicomOtherWord(tag)
                        : new DicomOtherWord(tag, (ushort[])GetTypedArray<ushort>(data));
                    break;
                case "PN":
                    item = new DicomPersonName(tag, Encoding.UTF8, GetString(data));
                    break;
                case "SH":
                    item = new DicomShortString(tag, Encoding.UTF8, GetString(data));
                    break;
                case "SL":
                    item = new DicomSignedLong(tag, (int[])GetTypedArray<int>(data));
                    break;
                case "SS":
                    item = new DicomSignedShort(tag, (short[])GetTypedArray<short>(data));
                    break;
                case "ST":
                    item = new DicomShortText(tag, Encoding.UTF8, GetString(data));
                    break;
                case "SQ":
                    item = GetDicomSequence(tag, data);
                    break;
                case "TM":
                    item = new DicomTime(tag, GetString(data));
                    break;
                case "UC":
                    item = new DicomUnlimitedCharacters(tag, Encoding.UTF8, GetString(data));
                    break;
                case "UI":
                    item = new DicomUniqueIdentifier(tag, GetString(data));
                    break;
                case "UL":
                    item = new DicomUnsignedLong(tag, (uint[])GetTypedArray<uint>(data));
                    break;
                case "UN":
                    item = data.IsBsonNull
                        ? new DicomUnknown(tag)
                        : new DicomUnknown(tag, (byte[])GetTypedArray<byte>(data));
                    break;
                case "UR":
                    item = new DicomUniversalResource(tag, Encoding.UTF8, GetString(data));
                    break;
                case "US":
                    item = new DicomUnsignedShort(tag, (ushort[])GetTypedArray<ushort>(data));
                    break;
                case "UT":
                    item = new DicomUnlimitedText(tag, Encoding.UTF8, GetString(data));
                    break;
                default:
                    throw new NotSupportedException($"Unsupported value representation {vr}");
            }

            return item;
        }

        private static DicomAttributeTag ParseAttributeTag(DicomTag tag, BsonValue bsonValue)
        {
            if (bsonValue.IsBsonNull)
                return new DicomAttributeTag(tag);

            var parsed = new List<DicomTag>();
            foreach (string subTagStr in bsonValue.AsString.Split('\\'))
            {
                ushort group = Convert.ToUInt16(subTagStr.Substring(0, 4), 16);
                ushort element = Convert.ToUInt16(subTagStr.Substring(4), 16);
                parsed.Add(new DicomTag(group, element));
            }
            return new DicomAttributeTag(tag, parsed.ToArray());
        }

        private static readonly BsonTypeMapperOptions _bsonTypeMapperOptions = new BsonTypeMapperOptions
        {
            MapBsonArrayTo = typeof(object[])
        };

        private static Array GetTypedArray<T>(BsonValue bsonValue) where T : struct
        {
            if (bsonValue == BsonNull.Value)
                return new T[0];

            BsonArray bsonArray = bsonValue.AsBsonArray;

            Array typedArray = new T[bsonArray.Count];
            var mappedBsonArray = (object[])BsonTypeMapper.MapToDotNetValue(bsonArray, _bsonTypeMapperOptions);

            // Some types have to be stored in larger representations for storage in MongoDB. Need to manually convert them back.
            if (typeof(T) == typeof(float))
                Array.Copy(mappedBsonArray.Select(Convert.ToSingle).ToArray(), typedArray, typedArray.Length);
            else if (typeof(T) == typeof(uint))
                Array.Copy(mappedBsonArray.Select(Convert.ToUInt32).ToArray(), typedArray, typedArray.Length);
            else if (typeof(T) == typeof(short))
                Array.Copy(mappedBsonArray.Select(Convert.ToInt16).ToArray(), typedArray, typedArray.Length);
            else if (typeof(T) == typeof(ushort))
                Array.Copy(mappedBsonArray.Select(Convert.ToUInt16).ToArray(), typedArray, typedArray.Length);
            // Otherwise we can convert normally
            else
                Array.Copy(mappedBsonArray, typedArray, typedArray.Length);

            return typedArray;
        }

        private static string GetString(BsonValue bsonValue)
        {
            return bsonValue.IsBsonNull ? null : bsonValue.AsString;
        }

        private static DicomSequence GetDicomSequence(DicomTag tag, BsonValue data)
        {
            return data.IsBsonNull
                ? new DicomSequence(tag)
                : new DicomSequence(tag, data.AsBsonArray.Select(x => BuildDicomDataset(x.AsBsonDocument)).ToArray());
        }

        #endregion
    }
}
