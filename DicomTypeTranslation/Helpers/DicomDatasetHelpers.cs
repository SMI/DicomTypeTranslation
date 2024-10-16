
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DicomTypeTranslation.Helpers;

/// <summary>
/// Helper methods for evaluating Equality between <see cref="DicomDataset"/> instances
/// </summary>
public static class DicomDatasetHelpers
{
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
    [UsedImplicitly]
    public static bool ValueEquals(DicomItem a, DicomItem b)
    {
        if (a == null || b == null)
            return a == b;

        if (a == b)
            return true;

        if (a.ValueRepresentation != b.ValueRepresentation || (uint)a.Tag != (uint)b.Tag)
            return false;

        return a switch
        {
            DicomElement element => b is DicomElement elementB && ValueEquals(element.Buffer, elementB.Buffer),
            DicomSequence sequence => b is DicomSequence sequenceB &&
                                      sequence.Items.Zip(sequenceB.Items, ValueEquals).All(static x => x),
            DicomFragmentSequence fragmentSequence => b is DicomFragmentSequence fragmentSequenceB &&
                                                      fragmentSequence.Fragments
                                                          .Zip(fragmentSequenceB.Fragments, ValueEquals)
                                                          .All(x => x),
            _ => a.Equals(b)
        };
    }

    /// <summary>
    /// Returns true if <paramref name="a"/> and <paramref name="b"/> are equal.  Supports <see cref="IBulkDataUriByteBuffer"/>, <see cref="EmptyBuffer"/>,
    /// <see cref="StreamByteBuffer"/> and <see cref="CompositeByteBuffer"/>.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static bool ValueEquals(IByteBuffer a, IByteBuffer b)
    {
        if (a == null || b == null)
            return ReferenceEquals(a,b);

        if (ReferenceEquals(a, b))
            return true;

        if (a.IsMemory)
            return b.IsMemory && a.Data.SequenceEqual(b.Data);

        switch (a)
        {
            case IBulkDataUriByteBuffer abuff:
            {
                if (b is not IBulkDataUriByteBuffer bbuff)
                    return false;

                return abuff.BulkDataUri == bbuff.BulkDataUri;
            }
            case EmptyBuffer when b is EmptyBuffer:
                return true;
            case StreamByteBuffer buffer when b is StreamByteBuffer bsbb:
            {
                var asbb = buffer;

                if (asbb.Stream == null || bsbb.Stream == null)
                    return asbb.Stream == bsbb.Stream;

                return asbb.Position == bsbb.Position && asbb.Size == bsbb.Size && asbb.Stream.Equals(bsbb.Stream);
            }
            case CompositeByteBuffer buffer when b is CompositeByteBuffer bufferB:
                return buffer.Buffers.Zip(bufferB.Buffers, ValueEquals).All(static x => x);
            default:
                return a.Equals(b);
        }
    }

    /// <summary>
    /// Compares two <see cref="DicomDataset"/>s for differences, taking the first input as the source for comparison.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="ignoreTrailingNull">If set, any differences due to a single trailing NUL character will be ignored</param>
    /// <returns>List of differences between the datasets</returns>
    [UsedImplicitly]
    [Obsolete("Throws exceptions in a lot of cases")]
    public static IEnumerable<string> Compare(DicomDataset a, DicomDataset b, bool ignoreTrailingNull = false)
    {
        if (a == null || b == null)
            throw new ArgumentException($"Dataset {(a == null ? "A" : "B")} was null");

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

        foreach (var item in a)
        {
            if (!b.Contains(item.Tag))
            {
                differences.Add($"B did not contain tag {item.Tag} {item.Tag.DictionaryEntry.Keyword} from A");
                continue;
            }

            if (item.ValueRepresentation.IsString)
            {
                var before = a.GetString(item.Tag);
                var after = b.GetString(item.Tag);

                if (string.Equals(before, after)) continue;

                if (ignoreTrailingNull && Math.Abs(before.Length - after.Length) == 1)
                {
                    var longest = before.Length > after.Length ? before : after;

                    // Check for a single trailing NUL character (int value == 0)
                    if (longest[^1] == 0)
                        continue;
                }

                differences.Add(string.Format("Tag {0} {1} {2} had value \"{3}\" in A and \"{4}\" in B",
                    item.Tag, item.ValueRepresentation, item.Tag.DictionaryEntry.Keyword, before, after));
            }
            else if (item.ValueRepresentation == DicomVR.SQ)
            {
                var seqA = a.GetSequence(item.Tag);
                var seqB = b.GetSequence(item.Tag);

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
                var valA = a.GetValues<object>(item.Tag);
                var valB = b.GetValues<object>(item.Tag);

                if (!(valA.Any() || valB.Any()))
                    continue;

                if (valA.Length != valB.Length)
                    differences.Add(string.Format("Tag {0} {1} {2} had {3} values in A and {4} values in B",
                        item.Tag, item.ValueRepresentation, item.Tag.DictionaryEntry.Keyword, valA.Length, valB.Length));

                var diffs = valA.Except(valB).ToList();

                if (!diffs.Any())
                    continue;

                differences.Add($"\tDifferent values were: {string.Join(", ", diffs)}");
            }
        }

        return differences;
    }
}