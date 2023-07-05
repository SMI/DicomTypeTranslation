
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FellowOakDicom;
using JetBrains.Annotations;
using MongoDB.Bson;


namespace DicomTypeTranslation;

/// <summary>
/// Helper class for rapidly writing <see cref="DicomTag"/> values into a <see cref="DicomDataset"/> in basic C# Types (string, int, double etc).  Also supports
/// Bson types (for MongoDb).
/// </summary>
public static class DicomTypeTranslaterWriter
{
    /// <summary>
    /// Methods to call to add the given Type to the dataset (requires casting due to generic T) and sometimes you have to call Add(a,b) sometimes only Add(b) works
    /// </summary>
    private static readonly Dictionary<Type, Action<DicomDataset, DicomTag, object>> DicomAddMethodDictionary = new();

    private static readonly Regex PrivateCreatorRegex = new(@":(.*)\)-");

    private static readonly string[] IgnoredBsonKeys = { "_id", "header" };


    static DicomTypeTranslaterWriter()
    {
        // Single types
        DicomAddMethodDictionary.Add(typeof(string), (ds, t, o) => ds.Add(t, (string)o));
        DicomAddMethodDictionary.Add(typeof(DicomTag), (ds, t, o) => ds.Add(t, (DicomTag)o));
        DicomAddMethodDictionary.Add(typeof(DateTime), (ds, t, o) => ds.Add(t, (DateTime)o));
        DicomAddMethodDictionary.Add(typeof(decimal), (ds, t, o) => ds.Add(t, (decimal)o));
        DicomAddMethodDictionary.Add(typeof(double), (ds, t, o) => ds.Add(t, (double)o));
        DicomAddMethodDictionary.Add(typeof(float), (ds, t, o) => ds.Add(t, (float)o));
        DicomAddMethodDictionary.Add(typeof(int), (ds, t, o) => ds.Add(t, (int)o));
        DicomAddMethodDictionary.Add(typeof(byte), (ds, t, o) => ds.Add(t, (byte)o));
        DicomAddMethodDictionary.Add(typeof(short), (ds, t, o) => ds.Add(t, (short)o));
        DicomAddMethodDictionary.Add(typeof(ushort), (ds, t, o) => ds.Add(t, (ushort)o));
        DicomAddMethodDictionary.Add(typeof(DicomDateRange), (ds, t, o) => ds.Add(t, (DicomDateRange)o));
        DicomAddMethodDictionary.Add(typeof(DicomDataset), (ds, t, o) => ds.Add(t, (DicomDataset)o));
        DicomAddMethodDictionary.Add(typeof(DicomUID), (ds, t, o) => ds.Add(t, (DicomUID)o));
        DicomAddMethodDictionary.Add(typeof(DicomTransferSyntax), (ds, t, o) => ds.Add(t, (DicomTransferSyntax)o));
        DicomAddMethodDictionary.Add(typeof(DicomSequence), (ds, _, o) => ds.Add((DicomSequence)o));
        DicomAddMethodDictionary.Add(typeof(uint), (ds, t, o) => ds.Add(t, (uint)o));
        DicomAddMethodDictionary.Add(typeof(long), (ds, t, o) => ds.Add(t, (long)o));

        // Array types
        DicomAddMethodDictionary.Add(typeof(string[]), (ds, t, o) => ds.Add(t, (string[])o));
        DicomAddMethodDictionary.Add(typeof(DicomTag[]), (ds, t, o) => ds.Add(t, (DicomTag[])o));
        DicomAddMethodDictionary.Add(typeof(DateTime[]), (ds, t, o) => ds.Add(t, (DateTime[])o));
        DicomAddMethodDictionary.Add(typeof(decimal[]), (ds, t, o) => ds.Add(t, (decimal[])o));
        DicomAddMethodDictionary.Add(typeof(double[]), (ds, t, o) => ds.Add(t, (double[])o));
        DicomAddMethodDictionary.Add(typeof(float[]), (ds, t, o) => ds.Add(t, (float[])o));
        DicomAddMethodDictionary.Add(typeof(int[]), (ds, t, o) => ds.Add(t, (int[])o));
        DicomAddMethodDictionary.Add(typeof(byte[]), (ds, t, o) => ds.Add(t, (byte[])o));
        DicomAddMethodDictionary.Add(typeof(short[]), (ds, t, o) => ds.Add(t, (short[])o));
        DicomAddMethodDictionary.Add(typeof(ushort[]), (ds, t, o) => ds.Add(t, (ushort[])o));
        DicomAddMethodDictionary.Add(typeof(DicomDateRange[]), (ds, t, o) => ds.Add(t, (DicomDateRange[])o));
        DicomAddMethodDictionary.Add(typeof(DicomDataset[]), (ds, t, o) => ds.Add(t, (DicomDataset[])o));
        DicomAddMethodDictionary.Add(typeof(DicomUID[]), (ds, t, o) => ds.Add(t, (DicomUID[])o));
        DicomAddMethodDictionary.Add(typeof(DicomTransferSyntax[]), (ds, t, o) => ds.Add(t, (DicomTransferSyntax[])o));
        DicomAddMethodDictionary.Add(typeof(DicomSequence[]), (ds, _, o) => ds.Add((DicomSequence[])o));
        DicomAddMethodDictionary.Add(typeof(uint[]), (ds, t, o) => ds.Add(t, (uint[])o));
        DicomAddMethodDictionary.Add(typeof(long[]), (ds, t, o) => ds.Add(t, (long[])o));

        //Those Involving something more complicated than simply forcing the generic <T> by casting something already of that Type
        DicomAddMethodDictionary.Add(typeof(TimeSpan), (ds, t, o) => ds.Add(t, TimeSpanToDate((TimeSpan)o)));
        DicomAddMethodDictionary.Add(typeof(TimeSpan[]), (ds, t, o) => ds.Add(t, ((TimeSpan[])o).Select(TimeSpanToDate).ToArray()));
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
        new DicomSetupBuilder().SkipValidation();
        switch (value)
        {
            case null:
                // Need to specify a type for the generic, even though it is ignored
                dataset.Add<string>(tag);
                return;
            //input is a dictionary of DicomTag=>Objects then it is a Sequence
            case Dictionary<DicomTag, object>[] sequenceArray:
                SetSequenceFromObject(dataset, tag, sequenceArray);
                return;
        }

        // Otherwise do generic add
        var key = (DicomAddMethodDictionary.ContainsKey(value.GetType())
                      ? value.GetType()
                      : DicomAddMethodDictionary.Keys.FirstOrDefault(k => k.IsInstanceOfType(value))) ??
                  throw new Exception($"No method to call for value type {value.GetType()}");
        DicomAddMethodDictionary[key](dataset, tag, value);
    }

    private static void SetSequenceFromObject(DicomDataset parentDataset, DicomTag tag, IEnumerable<Dictionary<DicomTag, object>> sequenceArray)
    {
        var sequenceList = new List<Dictionary<DicomTag, object>>(sequenceArray);

        var subDatasets = new List<DicomDataset>();

        foreach (var sequenceDict in sequenceList)
        {
            var subDataset = new DicomDataset();

            foreach (var kvp in sequenceDict)
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

        foreach (var element in document.Where(e => !IgnoredBsonKeys.Contains(e.Name)))
        {
            if (element.Name.Contains("PrivateCreator"))
            {
                var creatorTag = DicomTag.Parse(element.Name);
                dataset.Add(new DicomLongString(new DicomTag(creatorTag.Group, creatorTag.Element), element.Value["val"].AsString));
                continue;
            }

            var tag = TryParseTag(dataset, element);
            var vr = TryParseVr(element.Value);
            dataset.Add(vr == null
                ? CreateDicomItem(tag, element.Value)
                : CreateDicomItem(tag, element.Value["val"], vr));
        }

        return dataset;
    }

    private static DicomTag TryParseTag(DicomDataset dataset, BsonElement element)
    {
        var tagString = element.Name;

        try
        {
            var tag = tagString.StartsWith("(")
                ? DicomTag.Parse(tagString)
                : DicomDictionary.Default[tagString];

            if (!tag.IsPrivate)
                return tag;

            var creatorName = PrivateCreatorRegex.Match(element.Name).Groups[1].Value;
            tag = dataset.GetPrivateTag(new DicomTag(tag.Group, tag.Element, DicomDictionary.Default.GetPrivateCreator(creatorName)));

            return tag;
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Could not parse tag from string: {tagString}", e);
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
        vr ??= GetVrForTag(tag, data);

        DicomItem item = vr.Code switch
        {
            "AE" => new DicomApplicationEntity(tag, GetString(data)),
            "AS" => new DicomAgeString(tag, GetString(data)),
            "AT" => ParseAttributeTag(tag, data),
            "CS" => new DicomCodeString(tag, GetString(data)),
            "DA" => new DicomDate(tag, GetString(data)),
            "DS" => new DicomDecimalString(tag, GetString(data)),
            "DT" => new DicomDateTime(tag, GetString(data)),
            "FD" => new DicomFloatingPointDouble(tag, (double[])GetTypedArray<double>(data)),
            "FL" => new DicomFloatingPointSingle(tag, (float[])GetTypedArray<float>(data)),
            "IS" => new DicomIntegerString(tag, GetString(data)),
            "LO" => new DicomLongString(tag, GetString(data)),
            "LT" => new DicomLongText(tag, GetString(data)),
            "OB" => data.IsBsonNull ? new DicomOtherByte(tag) : new DicomOtherByte(tag, data.AsByteArray),
            "OD" => new DicomOtherDouble(tag, (double[])GetTypedArray<double>(data)),
            "OF" => new DicomOtherFloat(tag, (float[])GetTypedArray<float>(data)),
            "OL" => new DicomOtherLong(tag, (uint[])GetTypedArray<uint>(data)),
            "OV" => new DicomOtherVeryLong(tag, (ulong[])GetTypedArray<ulong>(data)),
            "OW" => data.IsBsonNull
                ? new DicomOtherWord(tag)
                : new DicomOtherWord(tag, (ushort[])GetTypedArray<ushort>(data)),
            "PN" => new DicomPersonName(tag, GetString(data)),
            "SH" => new DicomShortString(tag, GetString(data)),
            "SL" => new DicomSignedLong(tag, (int[])GetTypedArray<int>(data)),
            "SQ" => GetDicomSequence(tag, data),
            "SS" => new DicomSignedShort(tag, (short[])GetTypedArray<short>(data)),
            "ST" => new DicomShortText(tag, GetString(data)),
            "SV" => new DicomSignedVeryLong(tag, (long[])GetTypedArray<long>(data)),
            "TM" => new DicomTime(tag, GetString(data)),
            "UC" => new DicomUnlimitedCharacters(tag, GetString(data)),
            "UI" => new DicomUniqueIdentifier(tag, GetString(data)),
            "UL" => new DicomUnsignedLong(tag, (uint[])GetTypedArray<uint>(data)),
            "UN" => data.IsBsonNull
                ? new DicomUnknown(tag)
                : new DicomUnknown(tag, (byte[])GetTypedArray<byte>(data)),
            "UR" => new DicomUniversalResource(tag, GetString(data)),
            "US" => new DicomUnsignedShort(tag, (ushort[])GetTypedArray<ushort>(data)),
            "UT" => new DicomUnlimitedText(tag, GetString(data)),
            "UV" => new DicomUnsignedVeryLong(tag, (ulong[])GetTypedArray<ulong>(data)),
            _ => throw new NotSupportedException($"Unsupported value representation {vr}")
        };

        return item;
    }

    private static DicomAttributeTag ParseAttributeTag(DicomTag tag, BsonValue bsonValue)
    {
        if (bsonValue.IsBsonNull)
            return new DicomAttributeTag(tag);

        return new DicomAttributeTag(tag,
            bsonValue.AsString.Split('\\')
                .Select(subTagStr => new { subTagStr, group = Convert.ToUInt16(subTagStr[..4], 16) })
                .Select(t => new { t, element = Convert.ToUInt16(t.subTagStr[4..], 16) })
                .Select(t => new DicomTag(t.t.group, t.element)).ToArray());
    }

    private static readonly BsonTypeMapperOptions BsonTypeMapperOptions = new()
    {
        MapBsonArrayTo = typeof(object[])
    };

    private static Array GetTypedArray<T>(BsonValue bsonValue) where T : struct
    {
        if (bsonValue == BsonNull.Value)
            return Array.Empty<T>();

        var bsonArray = bsonValue.AsBsonArray;

        Array typedArray = new T[bsonArray.Count];
        var mappedBsonArray = (object[])BsonTypeMapper.MapToDotNetValue(bsonArray, BsonTypeMapperOptions);

        // Some types have to be stored in larger representations for storage in MongoDB. Need to manually convert them back.
        if (typeof(T) == typeof(float))
            Array.Copy(mappedBsonArray.Select(Convert.ToSingle).ToArray(), typedArray, typedArray.Length);
        else if (typeof(T) == typeof(uint))
            Array.Copy(mappedBsonArray.Select(Convert.ToUInt32).ToArray(), typedArray, typedArray.Length);
        else if (typeof(T) == typeof(short))
            Array.Copy(mappedBsonArray.Select(Convert.ToInt16).ToArray(), typedArray, typedArray.Length);
        else if (typeof(T) == typeof(ushort))
            Array.Copy(mappedBsonArray.Select(Convert.ToUInt16).ToArray(), typedArray, typedArray.Length);
        else if (typeof(T) == typeof(ulong))
        {
            // NOTE(rkm 2020-03-25) MongoDB only has a *signed* 64-bit integer type, so we have to special-case when dealing with *unsigned* 64-bit ints which are valid DICOM
            var tmp = new ulong[typedArray.Length];
            for (var i = 0; i < mappedBsonArray.Length; ++i)
                tmp[i] = BitConverter.ToUInt64(BitConverter.GetBytes((long)mappedBsonArray[i]), 0);
            Array.Copy(tmp, typedArray, typedArray.Length);
        }
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

    private static DicomVR GetVrForTag(DicomTag tag, BsonValue data)
    {
        try
        {
            return tag.IsPrivate && data is BsonArray ? DicomVR.SQ : tag.DictionaryEntry.ValueRepresentations.Single();
        }
        catch (InvalidOperationException e)
        {
            // Ok to throw an exception here - we should always be writing the VR into the Bson document if it's ambiguous
            e.Data["DicomTag"] += tag.DictionaryEntry.Keyword;
            throw;
        }
    }

    #endregion
}