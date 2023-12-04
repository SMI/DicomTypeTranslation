using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DicomTypeTranslation.Helpers;

/// <summary>
/// Helper methods for <see cref="Array"/> including equality and representation as strings
/// </summary>
public static class ArrayHelperMethods
{
    /// <summary>
    /// Returns true if the two arrays contain the same elements (using <see cref="FlexibleEquality"/>)
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool ArrayEquals(Array a, Array b)
    {
        if (a.Length != b.Length)
            return false;

        for (var i = 0; i < a.Length; i++)
            if (!FlexibleEquality.FlexibleEquals(a.GetValue(i), b.GetValue(i)))
                return false;

        return true;
    }

    /// <summary>
    /// Returns a string representation of the array suitable for human visualisation
    /// </summary>
    /// <param name="a"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static string AsciiArt(Array a, string prefix = "")
    {
        var sb = new StringBuilder();

        for (var i = 0; i < a.Length; i++)
        {
            sb.Append($"{prefix} [{i}] - ");

            //if run out of values in dictionary 1
            var val = a.GetValue(i) ?? "Null";

            if (DictionaryHelperMethods.IsDictionary(val))
                sb.AppendLine($"\r\n {DictionaryHelperMethods.AsciiArt((IDictionary)val, $"{prefix}\t")}");
            else if (val is Array array)
                sb.AppendLine($"\r\n {AsciiArt(array, $"{prefix}\t")}");
            else
                sb.AppendLine(val.ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a string representation of both arrays highlighting differences in array elements
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static string AsciiArt(Array a, Array b, string prefix = "")
    {
        var sb = new StringBuilder();

        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            sb.Append($"{prefix} [{i}] - ");

            //if run out of values in dictionary 1
            if (i > a.Length)
                sb.AppendLine(string.Format(" \t <NULL> \t {0}", b.GetValue(i)));
            //if run out of values in dictionary 2
            else if (i > b.Length)
                sb.AppendLine(string.Format(" \t {0} \t <NULL>", a.GetValue(i)));
            else
            {
                var val1 = a.GetValue(i);
                var val2 = b.GetValue(i);

                if (DictionaryHelperMethods.IsDictionary(val1) && DictionaryHelperMethods.IsDictionary(val2))
                    sb.Append($"\r\n {DictionaryHelperMethods.AsciiArt((IDictionary)val1,
                        (IDictionary)val2, $"{prefix}\t")}");
                else
                if (val1 is Array array1 && val2 is Array array2)
                    sb.Append($"\r\n {AsciiArt(array1,
                        array2, $"{prefix}\t")}");
                else
                    //if we haven't outrun of either array
                    sb.AppendLine(string.Format(" \t {0} \t {1} {2}",
                        val1,
                        val2,
                        FlexibleEquality.FlexibleEquals(val1, val2) ? "" : "<DIFF>"));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns true if <paramref name="a"/> contains any elements which are <see cref="Array"/> or <see cref="IDictionary"/>
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    private static bool ContainsSubArraysOrSubtrees(Array a)
    {
        return a.OfType<Array>().Any() || a.OfType<IDictionary>().Any();
    }

    /// <summary>
    /// Separates array elements with backslashes unless the array contains sub arrays or dictionaries in which case it resorts to ASCIIArt
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public static string GetStringRepresentation(Array a)
    {
        if (ContainsSubArraysOrSubtrees(a))
            return AsciiArt(a);

        var sb = new StringBuilder();
        // ReSharper disable once SuspiciousTypeConversion.Global - odd conversion but needed to get the right StringBuilder Append overload
        sb.AppendJoin('\\', (IEnumerable<object>)a);
        return sb.ToString();
    }
}