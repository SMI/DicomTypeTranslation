
using Dicom;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DicomTypeTranslation
{
    /// <summary>
    /// Helper class for rapidly reading <see cref="DicomTag"/> values from <see cref="DicomDataset"/> in basic C# Types (string, int, double etc).  Also supports
    /// Bson types (for MongoDb).
    /// </summary>
    public static class DicomTypeTranslaterReader
    {
        static DicomTypeTranslaterReader()
        {
            _csharpToBsonMappingDictionary = new Dictionary<Type, Func<object, BsonValue>>
            {
                {typeof(byte), (value) => new BsonBinaryData(new [] { (byte)value })},

                {typeof(DateTime), (value) => new BsonString(value.ToString()) },
                {typeof(TimeSpan), (value) => new BsonString(value.ToString()) },

                {typeof(decimal), (value) => new BsonDecimal128((decimal)value) },
                {typeof(double), (value) => new BsonDouble((double)value) },
                {typeof(float), (value) => new BsonDouble((float) value) },
                {typeof(int), (value) => new BsonInt32((int)value) },
                {typeof(short), (value) => new BsonInt32((short)value) },

                {typeof(string), (value) => new BsonString((string)value) },

                {typeof(uint), (value) => new BsonInt64((uint)value)},
                {typeof(ushort), (value) => new BsonInt32((ushort)value) },
            };
        }


        /// <summary>
        /// Returns a column name for a DicomTag either the Dicom standard keyword on it's own or the (group,element) tag number followed by the keyword.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="includeTagCodeAsPrefix">True to include the dicom tag code number e.g. '(0008,0058)-' before the keyword 'FailedSOPInstanceUIDList'</param>
        /// <returns></returns>
        public static string GetColumnNameForTag(DicomTag tag, bool includeTagCodeAsPrefix)
        {
            return includeTagCodeAsPrefix ?
                tag + "-" + tag.DictionaryEntry.Keyword :
                tag.DictionaryEntry.Keyword;
        }

        /// <summary>
        /// Returns a basic type (string, double, int, array, dictionary etc) for the given top level <paramref name="tag"/> in the <paramref name="dataset"/>.
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static object GetCSharpValue(DicomDataset dataset, DicomTag tag)
        {
            return GetCSharpValue(dataset, dataset.GetDicomItem<DicomItem>(tag));
        }

        /// <summary>
        /// Returns a basic type (string, double, int array, dictionary etc) for the given <paramref name="item"/> in the <paramref name="dataset"/>.
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static object GetCSharpValue(DicomDataset dataset, DicomItem item)
        {
            if (dataset == null || !dataset.Any())
                throw new ArgumentException("The DicomDataset is invalid as it is null or has no elements.");

            if (item == null || item.Tag == null || item.ValueRepresentation == null)
                throw new ArgumentException("The DicomItem is invalid as it is either null, has a null Tag, or null ValueRepresentation: " + item);

            if (!dataset.Contains(item))
                throw new ArgumentException("The DicomDataset does not contain the item");

            if (item.Tag == DicomTag.PixelData)
                return null;

            switch (item.ValueRepresentation.Code)
            {
                // AE - Application Entity
                case "AE":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // AS - Age String
                case "AS":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // AT - Attribute Tag
                case "AT":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // CS - Code String
                case "CS":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // DA - Date
                case "DA":
                    return GetValueFromDatasetWithMultiplicity<DateTime>(dataset, item.Tag);

                // DS - Decimal String
                case "DS":
                    return GetValueFromDatasetWithMultiplicity<decimal>(dataset, item.Tag);

                // DT - Date Time
                case "DT":
                    return GetValueFromDatasetWithMultiplicity<DateTime>(dataset, item.Tag);

                // FL - Floating Point Single
                case "FL":
                    return GetValueFromDatasetWithMultiplicity<float>(dataset, item.Tag);

                // FD - Floating Point Double
                case "FD":
                    return GetValueFromDatasetWithMultiplicity<double>(dataset, item.Tag);

                // IS - Integer String
                case "IS":
                    return GetValueFromDatasetWithMultiplicity<int>(dataset, item.Tag);

                // LO - Long String
                case "LO":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // LT - Long Text
                case "LT":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // OB - Other Byte String
                case "OB":
                    return GetValueFromDatasetWithMultiplicity<byte>(dataset, item.Tag);

                // OD - Other Double String
                case "OD":
                    return GetValueFromDatasetWithMultiplicity<double>(dataset, item.Tag);

                // OF - Other Float String
                case "OF":
                    return GetValueFromDatasetWithMultiplicity<float>(dataset, item.Tag);

                // OL - Other Long
                case "OL":
                    return GetValueFromDatasetWithMultiplicity<uint>(dataset, item.Tag);

                // OW - Other Word String
                case "OW":
                    return GetValueFromDatasetWithMultiplicity<ushort>(dataset, item.Tag);

                // PN - Person Name
                case "PN":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // SH - Short String
                case "SH":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // SL - Signed Long
                case "SL":
                    return GetValueFromDatasetWithMultiplicity<int>(dataset, item.Tag);

                // SQ - Sequence
                case "SQ":
                    return GetSequenceFromDataset(dataset, item.Tag);

                // SS - Signed Short
                case "SS":
                    return GetValueFromDatasetWithMultiplicity<short>(dataset, item.Tag);

                // ST - Short Text
                case "ST":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // TM - Time
                case "TM":

                    var tm = GetValueFromDatasetWithMultiplicity<DateTime>(dataset, item.Tag);

                    // Need to handle case where we couldn't parse to DateTime so returned string instead
                    return tm is DateTime
                            ? ConvertToTimeSpanArray(tm)
                            : tm;

                // UC - Unlimited Characters
                case "UC":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // UI - Unique Identifier
                case "UI":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // UL - Unsigned Long
                case "UL":
                    return GetValueFromDatasetWithMultiplicity<uint>(dataset, item.Tag);

                // UN - Unknown
                case "UN":
                    return GetValueFromDatasetWithMultiplicity<byte>(dataset, item.Tag);

                // UR - URL
                case "UR":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // US - Unsigned Short
                case "US":
                    return GetValueFromDatasetWithMultiplicity<ushort>(dataset, item.Tag);

                // UT - Unlimited Text
                case "UT":
                    return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

                // NONE
                case "NONE":
                    return GetValueFromDatasetWithMultiplicity<object>(dataset, item.Tag);

                default:
                    //return GetValueFromDatasetWithMultiplicity<object>(dataset, item.Tag);
                    throw new Exception("Unknown VR code: " +
                                        item.ValueRepresentation.Code +
                                        "(" + item.ValueRepresentation.Name + ")");
            }
        }

        private static object ConvertToTimeSpanArray(object array)
        {
            if (array == null)
                return null;

            if (array is DateTime)
                return ((DateTime)array).TimeOfDay;

            return ((DateTime[])array)
                    .Select(e => e.TimeOfDay)
                    .ToArray();
        }

        private static object GetSequenceFromDataset(DicomDataset ds, DicomTag tag)
        {
            var toReturn = new List<Dictionary<DicomTag, object>>();

            foreach (DicomDataset sequenceElement in ds.GetSequence(tag))
            {
                IEnumerator<DicomItem> enumerator = sequenceElement.GetEnumerator();

                var current = new Dictionary<DicomTag, object>();
                toReturn.Add(current);

                while (enumerator.MoveNext())
                    current.Add(enumerator.Current.Tag, GetCSharpValue(sequenceElement, enumerator.Current));
            }

            return toReturn.Count != 0
                ? toReturn.ToArray()
                : null;
        }

        private static object GetValueFromDatasetWithMultiplicity<TNaturalType>(DicomDataset dataset, DicomTag tag)
        {
            Array array = dataset.GetValues<TNaturalType>(tag);

            if (array == null || array.Length == 0)
                return null;

            //if it is a single element then although the tag supports multiplicity only 1 value is stored in it so return string
            if (array.Length == 1)
                return array.GetValue(0);

            //tag supports multiplicity and the item has multiple values stored in it
            return array;
        }

        #region Bson Types

        private static readonly Dictionary<Type, Func<object, BsonValue>> _csharpToBsonMappingDictionary;

        /// <summary>
        /// Returns a key for a DicomTag either the Dicom standard keyword on it's own or the (group,element) tag number followed by the keyword. Strips out any "." for MongoDb. 
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static string GetBsonKeyForTag(DicomTag tag)
        {
            string tagName =
                (tag.IsPrivate || tag.DictionaryEntry.MaskTag != null) ?
                GetColumnNameForTag(tag, true) :
                GetColumnNameForTag(tag, false);

            // Can't have "." in MongoDb keys
            return tagName.Replace(".", "_");
        }

        private static BsonValue GetBsonValue(object val)
        {
            if (_csharpToBsonMappingDictionary.ContainsKey(val.GetType()))
                return _csharpToBsonMappingDictionary[val.GetType()](val);

            throw new ApplicationException("Couldn't get Bson value for item: " + val + "of type: " + val.GetType());
        }

        /// <summary>
        /// Returns a Bson object that represents basic typed object <paramref name="val"/>
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static BsonValue CreateBsonValue(object val)
        {
            if (val == null)
                return BsonNull.Value;

            // Sequences
            if (val is Dictionary<DicomTag, object> asDict)
            {
                var subDoc = new BsonDocument();

                foreach (KeyValuePair<DicomTag, object> subItem in asDict)
                    subDoc.Add(GetBsonKeyForTag(subItem.Key), CreateBsonValue(subItem.Value));

                return subDoc;
            }

            // Multiplicity
            if (!(val is Array asArray))
                return GetBsonValue(val);

            bool isDictArray = asArray.GetType().GetElementType() == typeof(Dictionary<DicomTag, object>);

            var arr = new BsonArray();

            foreach (object item in asArray)
                arr.Add(isDictArray
                            ? CreateBsonValue(item)
                            : GetBsonValue(item));

            return arr;
        }

        private static BsonArray CreateBsonValueFromSequence(DicomDataset ds, DicomTag tag)
        {
            if (!ds.Contains(tag))
                throw new ArgumentException("The DicomDataset does not contain the item");

            var sequenceArray = new BsonArray();

            foreach (DicomDataset sequenceElement in ds.GetSequence(tag))
                sequenceArray.Add(BuildDatasetDocument(sequenceElement));

            return sequenceArray;
        }

        /// <summary>
        /// Create a single BsonValue from a DicomItem
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private static BsonValue CreateBsonValue(DicomDataset dataset, DicomItem item)
        {
            // Handle some special cases first of all

            if (item as DicomSequence != null)
                return CreateBsonValueFromSequence(dataset, item.Tag);

            var element = dataset.GetDicomItem<DicomElement>(item.Tag);

            if (element.Count == 0)
                return BsonNull.Value;

            if (element.ValueRepresentation.IsString)
                return (BsonString)dataset.GetString(element.Tag);

            if (element.ValueRepresentation == DicomVR.AT)
                return (BsonString)string.Join("\\", dataset.GetValues<string>(element.Tag));

            // Else do a general conversion

            object cSharpValue = GetCSharpValue(dataset, element);

            return cSharpValue == null
                    ? BsonNull.Value
                    : CreateBsonValue(cSharpValue);
        }

        /// <summary>
        /// Build an entire BsonDocument from a dataset
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public static BsonDocument BuildDatasetDocument(DicomDataset dataset)
        {
            var datasetDoc = new BsonDocument();

            foreach (DicomItem item in dataset)
            {
                string bsonKey = GetBsonKeyForTag(item.Tag);
                BsonValue bsonVal = CreateBsonValue(dataset, item);

                datasetDoc.Add(bsonKey, bsonVal);
            }

            return datasetDoc;
        }

        #endregion
    }
}