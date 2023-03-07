using System;
using System.Collections;

namespace DicomTypeTranslation.Helpers
{
    /// <summary>
    /// Equality helper that considers <see cref="IDictionary"/> and <see cref="Array"/> as 'equal' if their elements
    /// are equal (includes recursion support for nested collections e.g. array of arrays).
    /// </summary>
    public static class FlexibleEquality
    {
        /// <summary>
        /// Returns true if <paramref name="a"/> and <paramref name="b"/> are Equal or are collections (<see cref="Array"/> / <see cref="IDictionary"/>) with equal
        /// elements (includes recursion support for nested collections e.g. arrays of arrays).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool FlexibleEquals(object a, object b)
        {
            if (a == null || b == null)
                return ReferenceEquals(a,b);

        //types are different so most likely we are not equipped to deal with this problem let a decide if it is equal or not
        if (a.GetType() != b.GetType())
            return a.Equals(b);

        //if they are both dictionaries
        if (DictionaryHelperMethods.IsDictionary(a) && DictionaryHelperMethods.IsDictionary(b))
            //and they are dictionary equals
            return DictionaryHelperMethods.DictionaryEquals((IDictionary)a, (IDictionary)b);

            //if they are both arrays
            if (a is Array array)
                return ArrayHelperMethods.ArrayEquals(array, (Array)b);

            //they are not dictionaries or arrays
            return Equals(a, b);
        }
    }
}
