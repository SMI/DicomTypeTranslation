using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DicomTypeTranslation.Helpers
{
    /// <summary>
    /// Helper methdos for interacting with <see cref="IDictionary"/> objects including equality and representation as string
    /// </summary>
    public static class DictionaryHelperMethods
    {
        /// <summary>
        /// Returns true if <paramref name="o"/> is an <see cref="IDictionary"/>
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsDictionary(object o)
        {
            return o is IDictionary;
        }

        /// <summary>
        /// Returns true if <paramref name="t"/> is a assignable to <see cref="IDictionary"/>
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsDictionary(Type t)
        {
            return typeof(IDictionary).IsAssignableFrom(t);
        }

        /// <summary>
        /// Determines whether the two dictionaries contain the same keys and values (using <see cref="FlexibleEquality"/>).  Handles any generic dictionary and uses
        /// Equals for comparison.  Note that this will handle Values that are sub dictionaries (recursively calling <see cref="DictionaryEquals"/>) but will not handle
        /// when keys are dictionaries.
        /// </summary>
        /// <param name="dict1"></param>
        /// <param name="dict2"></param>
        /// <returns>true if the keys/values are Equal and neither contains novel keys</returns>
        public static bool DictionaryEquals(IDictionary dict1, IDictionary dict2)
        {
            //if either is null
            if (dict1 == null || dict2 == null)
                return Object.ReferenceEquals(dict1,dict2); //they are only equal if they are both null

            var keys1 = new HashSet<object>();

            foreach (var k in dict1.Keys)
                keys1.Add(k);

            var keys2 = new HashSet<object>();

            foreach (var k in dict2.Keys)
                keys2.Add(k);

            //they do not contain the same keys
            if (!keys1.SetEquals(keys2))
                return false;

            //do all the key value pairs in dictionary 1 match dictionary 2
            foreach (var key in keys1)
                if (!FlexibleEquality.FlexibleEquals(dict1[key], dict2[key]))
                    return false;

            //they keys are the same set and the values are Equal too
            return true;
        }

        /// <summary>
        /// Returns a hashcode for the given <paramref name="dict"/> which is based on the elments.  This should be used when 
        /// using <see cref="DictionaryEquals(IDictionary, IDictionary)"/> for equality to ensure 'equal' instances have the same hashcode.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static int GetHashCode<TKey, TValue>(Dictionary<TKey, TValue> dict)
        {
            if (dict == null)
                return 0;

            if (!dict.Any())
                return 0;

            unchecked
            {
                var hashCode = dict.Keys.First().GetHashCode();
                foreach (var kvp in dict)
                {
                    hashCode = (hashCode * 397) ^ kvp.Key.GetHashCode();
                    hashCode = (hashCode * 397) ^ (kvp.Value != null ? kvp.Value.GetHashCode() : 0);
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Returns a string representation of the <paramref name="dict"/> suitable for human visualisation
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string AsciiArt(IDictionary dict, string prefix = "")
        {
            var sb = new StringBuilder();

            var keys1 = dict.Keys.Cast<object>().OrderBy(i => i).ToList();

            for (var i = 0; i < keys1.Count; i++)
            {
                sb.Append(prefix);

                //if run out of values in dictionary 1
                var val = dict[keys1[i]];

                if (val is Array)
                    sb.Append($" {keys1[i]} : \r\n {ArrayHelperMethods.AsciiArt((Array)val, $"{prefix}\t")}");
                else
                    //if both are dictionaries
                    if (IsDictionary(val))
                    sb.Append($" {keys1[i]} : \r\n {AsciiArt((IDictionary)val, $"{prefix}\t")}");
                else
                    //if we haven't outrun of either array
                    sb.AppendLine($" {keys1[i]} - \t {val}");

            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a string representation of both <see cref="IDictionary"/>s highlighting differences in array elements
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="dict2"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string AsciiArt(IDictionary dict, IDictionary dict2, string prefix = "")
        {
            var sb = new StringBuilder();

            var keys1 = dict.Keys.Cast<object>().OrderBy(i => i).ToList();
            var keys2 = dict2.Keys.Cast<object>().OrderBy(i => i).ToList();

            for (var i = 0; i < Math.Max(keys1.Count, keys2.Count); i++)
            {
                sb.Append($"{prefix}[{i}] - ");

                //if run out of values in dictionary 1
                if (i > keys1.Count)
                    sb.AppendLine($" {keys2[i]} - \t <NULL> \t {dict2[keys2[i]]}");
                //if run out of values in dictionary 2
                else if (i > keys2.Count)
                    sb.AppendLine($" {keys1[i]} - \t {dict[keys1[i]]} \t <NULL>");
                else
                {
                    var val1 = dict[keys1[i]];
                    var val2 = dict2[keys2[i]];

                    if (val1 is Array && val2 is Array)
                        sb.Append(
                            $" {keys1[i]} : \r\n {ArrayHelperMethods.AsciiArt((Array)val1, (Array)val2, $"{prefix}\t")}");
                    else
                        //if both are dictionaries
                        if (IsDictionary(val1) && IsDictionary(val2))
                        sb.Append($" {keys1[i]} : \r\n {AsciiArt((IDictionary)val1, (IDictionary)val2, $"{prefix}\t")}");
                    else
                        //if we haven't outrun of either array
                        sb.AppendLine(
                            $" {keys1[i]} - \t {dict[keys1[i]]} \t {dict2[keys2[i]]} {(FlexibleEquality.FlexibleEquals(dict[keys1[i]], dict2[keys2[i]]) ? "" : "<DIFF>")}");
                }
            }

            return sb.ToString();
        }
    }
}
