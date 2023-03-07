
using System;
using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using MongoDB.Bson;


namespace DicomTypeTranslation
{
    /// <summary>
    /// Helper class for rapidly reading <see cref="DicomTag"/> values from <see cref="DicomDataset"/> in basic C# Types (string, int, double etc).  Also supports
    /// Bson types (for MongoDb).
    /// </summary>
    public static class DicomTypeTranslaterReader
    {

        /// <summary>
        /// Returns a column name for a DicomTag either the Dicom standard keyword on it's own or the (group,element) tag number followed by the keyword.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="includeTagCodeAsPrefix">True to include the dicom tag code number e.g. '(0008,0058)-' before the keyword 'FailedSOPInstanceUIDList'</param>
        /// <returns></returns>
        public static string GetColumnNameForTag(DicomTag tag, bool includeTagCodeAsPrefix)
        {
            return includeTagCodeAsPrefix ? $"{tag}-{tag.DictionaryEntry.Keyword}"
                :
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
                throw new ArgumentException(
                    $"The DicomItem is invalid as it is either null, has a null Tag, or null ValueRepresentation: {item}");

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

                // OV - Other Very Long
                case "OV":
                    return GetValueFromDatasetWithMultiplicity<ulong>(dataset, item.Tag);

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

                // SV - Signed Very Long
                case "SV":
                    return GetValueFromDatasetWithMultiplicity<long>(dataset, item.Tag);

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

                // UV - Unsigned Very Long
                case "UV":
                    return GetValueFromDatasetWithMultiplicity<ulong>(dataset, item.Tag);

                // NONE
                case "NONE":
                    return GetValueFromDatasetWithMultiplicity<object>(dataset, item.Tag);

                default:
                    //return GetValueFromDatasetWithMultiplicity<object>(dataset, item.Tag);
                    throw new Exception(
                        $"Unknown VR code: {item.ValueRepresentation.Code}({item.ValueRepresentation.Name})");
            }
        }

        private static object ConvertToTimeSpanArray(object array)
        {
            return array switch
            {
                null => null,
                DateTime time => time.TimeOfDay,
                _ => ((DateTime[])array).Select(e => e.TimeOfDay).ToArray()
            };
        }

        private static object GetSequenceFromDataset(DicomDataset ds, DicomTag tag)
        {
            var toReturn = new List<Dictionary<DicomTag, object>>();

            foreach (var sequenceElement in ds.GetSequence(tag))
            {
                var enumerator = sequenceElement.GetEnumerator();

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
            Array array;

            try
            {
                array = dataset.GetValues<TNaturalType>(tag);
            }
            catch (Exception e)
            {
                var vals = dataset.GetString(tag);
                throw new ArgumentException($"Tag {tag.DictionaryEntry.Keyword} {tag} has invalid value(s): '{vals}'", e);
            }

            if (array == null || array.Length == 0)
                return null;

            //if it is a single element then although the tag supports multiplicity only 1 value is stored in it so return string
            // or tag supports multiplicity and the item has multiple values stored in it
            return array.Length == 1 ? array.GetValue(0) : array;
        }

        #region Bson Types

        /// <summary>
        /// Returns a key for a DicomTag either the Dicom standard keyword on it's own or the (group,element) tag number followed by the keyword. Strips out any "." for MongoDb. 
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static string GetBsonKeyForTag(DicomTag tag)
        {
            var tagName =
                tag.IsPrivate || tag.DictionaryEntry.MaskTag != null ?
                GetColumnNameForTag(tag, true) :
                GetColumnNameForTag(tag, false);

            // Can't have "." in MongoDb keys
            return tagName.Replace(".", "_");
        }

        private static BsonValue CreateBsonValueFromSequence(DicomDataset ds, DicomTag tag, bool writeVr)
        {
            if (!ds.Contains(tag))
                throw new ArgumentException("The DicomDataset does not contain the item");

            var sequenceArray = new BsonArray();

            foreach (var sequenceElement in ds.GetSequence(tag))
                sequenceArray.Add(BuildBsonDocument(sequenceElement));

            if (sequenceArray.Count > 0)
                return sequenceArray;

            return writeVr
                ? (BsonValue)new BsonDocument
                    {
                        { "vr", "SQ" },
                        { "val", BsonNull.Value }
                    }
                : BsonNull.Value;
        }

        /// <summary>
        /// Create a single BsonValue from a DicomItem
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="item"></param>
        /// <param name="writeVr"></param>
        /// <returns></returns>
        private static BsonValue CreateBsonValue(DicomDataset dataset, DicomItem item, bool writeVr)
        {
            if (item is DicomSequence)
                return CreateBsonValueFromSequence(dataset, item.Tag, writeVr);

            var element = dataset.GetDicomItem<DicomElement>(item.Tag);

            BsonValue retVal;

            if (element is null || element.Count == 0)
                retVal = BsonNull.Value;

            else if (!DicomTypeTranslater.SerializeBinaryData && DicomTypeTranslater.DicomVrBlacklist.Contains(item.ValueRepresentation))
                retVal = BsonNull.Value;

            else if (element is DicomStringElement)
            {
                if (element is not DicomMultiStringElement && element.Length == 0)
                    retVal = BsonNull.Value;
                else
                    retVal = (BsonString)dataset.GetString(element.Tag);
            }

            else if (element.ValueRepresentation == DicomVR.AT) // Special case - need to construct manually
                retVal = GetAttributeTagString(dataset, element.Tag);

            else
            {
                // Must be a numeric element - convert using default BSON mapper
                var val = dataset.GetValues<object>(item.Tag);
                retVal = BsonTypeMapper.MapToBsonValue(val);
            }

            if (!writeVr)
                return retVal;

            return new BsonDocument
            {
                { "vr", item.ValueRepresentation.Code },
                { "val", retVal }
            };
        }

        private static BsonValue GetAttributeTagString(DicomDataset dataset, DicomTag tag)
        {
            return (BsonString)string
                .Join("\\", dataset.GetValues<string>(tag))
                .Replace("(", string.Empty)
                .Replace(",", string.Empty)
                .Replace(")", string.Empty)
                .ToUpper();
        }

        /// <summary>
        /// Build an entire BsonDocument from a dataset
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public static BsonDocument BuildBsonDocument(DicomDataset dataset)
        {
            var datasetDoc = new BsonDocument();

            foreach (var item in dataset)
            {
                // Don't serialize group length elements
                if (((uint)item.Tag & 0xffff) == 0)
                    continue;

                var bsonKey = GetBsonKeyForTag(item.Tag);

                // For private tags, or tags which have an ambiguous ValueRepresentation, we need to include the VR as well as the value
                var writeVr =
                    item.Tag.IsPrivate ||
                    item.Tag.DictionaryEntry.ValueRepresentations.Length > 1;

                var bsonVal = CreateBsonValue(dataset, item, writeVr);

                datasetDoc.Add(bsonKey, bsonVal);
            }

            return datasetDoc;
        }

        #endregion
    }
}