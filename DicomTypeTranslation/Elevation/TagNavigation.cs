using System;
using FellowOakDicom;
using DicomTypeTranslation.Elevation.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace DicomTypeTranslation.Elevation;

internal class TagNavigation
{
    //do not make this public because it breaks constraints on SQ/non SQ in constructor
    public readonly bool IsLast;
    private readonly DicomTag _tag;

    public TagNavigation(string navigationToken, bool isLast)
    {
        IsLast = isLast;

        var entry = DicomDictionary.Default.FirstOrDefault(t => t.Keyword == navigationToken);

        if (entry == null)
            throw new TagNavigationException($"Unknown DICOM tag '{navigationToken}'");

        if (!isLast)
        {
            if (entry.ValueRepresentations.All(v => v != DicomVR.SQ))
                throw new TagNavigationException(
                    $"Navigation Token {navigationToken} was not the final token in the pathway therefore it must support DicomVR.SQ (otherwise how can we get a subsequence)");
        }
        else
        {
            if (entry.ValueRepresentations.All(v => v == DicomVR.SQ))
                throw new TagNavigationException(
                    $"Navigation Token {navigationToken} was the final token in the pathway so cannot be DicomVR.SQ");
        }

        _tag = entry.Tag;
    }



    public IEnumerable<SequenceElement> GetSubsets(DicomDataset dataset)
    {
        return !dataset.Contains(_tag) ? Array.Empty<SequenceElement>() : ToSequenceElementArray((Dictionary<DicomTag, object>[])DicomTypeTranslaterReader.GetCSharpValue(dataset, _tag), null);
    }

    public IEnumerable<SequenceElement> GetSubset(SequenceElement location)
    {
        return location.Dataset.ContainsKey(_tag) ? ToSequenceElementArray((Dictionary<DicomTag, object>[])location.Dataset[_tag], location) : Array.Empty<SequenceElement>();
    }

    private SequenceElement[] ToSequenceElementArray(IEnumerable<Dictionary<DicomTag, object>> getCSharpValue, SequenceElement location)
    {
        if (getCSharpValue == null)
            return Array.Empty<SequenceElement>();

        var toReturn = getCSharpValue.Select(s => new SequenceElement(_tag, s, location)).ToArray();

        //tell the SequenceElement about all the other elements (including itself) which appear side by side as array siblings in the sequence
        foreach (var element in toReturn)
            element.ArraySiblings.AddRange(toReturn);

        return toReturn;
    }

    /// <summary>
    /// Returns tag (which must be non Sequence tags) value contained in the current sequence (<see cref="SequenceElement"/>).  Optionally only match those that pass the conditional
    /// </summary>
    /// <param name="sequenceElement"></param>
    /// <param name="conditional"></param>
    /// <returns></returns>
    public object GetTags(SequenceElement sequenceElement, TagRelativeConditional conditional)
    {
        if (!sequenceElement.Dataset.ContainsKey(_tag)) return null;

        if (conditional == null || conditional.IsMatch(sequenceElement, _tag))
            return sequenceElement.Dataset[_tag];

        return null;
    }
}