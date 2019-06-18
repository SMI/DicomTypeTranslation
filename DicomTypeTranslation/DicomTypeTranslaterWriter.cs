
using Dicom;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

            // Build Bson VR mapping dictionary
            _getTypeForValueVr = new Dictionary<DicomVR, Func<object, object>>
            {
                { DicomVR.FD, o => Convert.ToDouble(o) },
                { DicomVR.FL, o => Convert.ToSingle(o) },
                { DicomVR.SL, o => Convert.ToInt32(o)  },
                { DicomVR.SS, o => Convert.ToInt16(o)  },
                { DicomVR.UL, o => Convert.ToUInt32(o) },
                { DicomVR.US, o => Convert.ToUInt16(o) },
                { DicomVR.OW, o => Convert.ToUInt16(o) },
                { DicomVR.OD, o => Convert.ToDouble(o) },
                { DicomVR.OF, o => Convert.ToSingle(o) },
                { DicomVR.OL, o => Convert.ToUInt32(o) },
                { DicomVR.OB, o => ConvertBsonBytes(o) },
                { DicomVR.UN, o => ConvertBsonBytes(o) }
            };
        }

        private static DateTime TimeSpanToDate(TimeSpan ts)
        {
            return new DateTime(1, 1, 1, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
        }

        /// <inheritdoc cref="SetDicomTag(DicomDataset, DicomTag, object)"/>
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

            //otherwise do generic add
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

        private static readonly BsonTypeMapperOptions _bsonTypeMapperOptions = new BsonTypeMapperOptions
        {
            MapBsonArrayTo = typeof(object[])
        };

        private static readonly Dictionary<DicomVR, Func<object, object>> _getTypeForValueVr;

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

                //TODO Error handling
                string creatorName = _privateCreatorRegex.Match(element.Name).Groups[1].Value;
                tag = dataset.GetPrivateTag(new DicomTag(tag.Group, tag.Element, DicomDictionary.Default.GetPrivateCreator(creatorName)));

                return tag;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Could not parse tag from string: " + tagString, e);
            }
        }

        private static byte ConvertBsonBytes(object o)
        {
            var asByteArray = o as byte[];

            if (asByteArray == null || asByteArray.Length != 1)
                throw new ApplicationException("Invalid byte content");

            return asByteArray[0];
        }

        private static bool IsSmiIdentifier(BsonValue bsonValue)
        {
            var asBsonString = bsonValue as BsonString;
            return asBsonString != null && ((string)asBsonString).StartsWith("SMI:");
        }

        private static object GetObjectFromBsonValue(DicomDataset dataset, BsonValue bsonValue, DicomVR[] possibleVrs)
        {
            if (bsonValue == BsonNull.Value)
                return null;

            if (IsSmiIdentifier(bsonValue))
                return null;

            if (possibleVrs[0] == DicomVR.SQ)
                return BuildSequenceDictionaryFromBsonArray(dataset, (BsonArray)bsonValue);

            if (possibleVrs.Length == 1 && !_getTypeForValueVr.ContainsKey(possibleVrs[0]))
                return BsonTypeMapper.MapToDotNetValue(bsonValue);

            var asArray = bsonValue as BsonArray;

            if (asArray == null)
                return TryParseMultipleVrs(bsonValue, possibleVrs);

            return TryParseMultipleVrs(asArray, possibleVrs);
        }

        private static object TryParseMultipleVrs(BsonValue bsonValue, DicomVR[] possibleVrs)
        {
            return TryParseMultipleVrs(new BsonArray { bsonValue }, possibleVrs).GetValue(0);
        }

        private static Array TryParseMultipleVrs(BsonArray bsonArray, DicomVR[] possibleVrs)
        {
            var asObjectArray = (object[])BsonTypeMapper.MapToDotNetValue(bsonArray, _bsonTypeMapperOptions);

            foreach (DicomVR vr in possibleVrs)
            {
                try
                {
                    object[] convertedItems = asObjectArray.Select(_getTypeForValueVr[vr]).ToArray();

                    Array typedItems = Array.CreateInstance(convertedItems[0].GetType(), convertedItems.Length);
                    Array.Copy(convertedItems, typedItems, convertedItems.Length);

                    return typedItems;
                }
                catch (Exception) { /* Ignored */ }
            }

            // Only reach here if we can't parse the array items into any of the types for the given VRs
            throw new Exception("Couldn't parse BsonArray to dicom values");
        }

        private static Dictionary<DicomTag, object>[] BuildSequenceDictionaryFromBsonArray(DicomDataset dataset, BsonArray bsonArray)
        {
            var subDatasets = new List<Dictionary<DicomTag, object>>();

            foreach (BsonValue arrayItem in bsonArray)
            {
                var asSubDocument = arrayItem as BsonDocument;

                if (asSubDocument == null)
                    throw new ApplicationException("Array element was not a BsonDocument");

                var subDictionary = new Dictionary<DicomTag, object>();

                foreach (BsonElement subItem in asSubDocument)
                {
                    DicomTag tag = TryParseTag(dataset, subItem);
                    object value = GetObjectFromBsonValue(dataset, subItem.Value, tag.DictionaryEntry.ValueRepresentations);

                    subDictionary.Add(tag, value);
                }

                subDatasets.Add(subDictionary);
            }

            return subDatasets.ToArray();
        }

        private static void AddPrivateCreator(DicomDataset dataset, BsonElement element)
        {
            DicomTag tempTag = DicomTag.Parse(element.Name);
            dataset.Add(new DicomCodeString(new DicomTag(tempTag.Group, tempTag.Element), element.Value.AsString));
        }

        /// <summary>
        /// Converts the <paramref name="document"/> into a <see cref="DicomDataset"/>
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static DicomDataset BuildDatasetFromBsonDocument(BsonDocument document)
        {
            var dataset = new DicomDataset();

            document.Remove("_id");
            document.Remove("header");

            foreach (BsonElement element in document)
            {
                if (element.Name.Contains("PrivateCreator"))
                {
                    AddPrivateCreator(dataset, element);
                    continue;
                }

                DicomTag tag = TryParseTag(dataset, element);
                SetDicomTag(dataset, tag, GetObjectFromBsonValue(dataset, element.Value, tag.DictionaryEntry.ValueRepresentations));
            }

            return dataset;
        }

        #endregion
    }
}
