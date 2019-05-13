using System;
using System.Collections;

namespace DicomTypeTranslation.Helpers
{
    public static class FlexibleEquality
    {
        public static bool FlexibleEquals(object a, object b)
        {
            if (a == null || b == null)
                return a == b;

            //types are different so most likely we are not equipped to deal with this problem let a decide if it is equal or not
            if (a.GetType() != b.GetType())
                return a.Equals(b);

            //if they are both dictionaries
            if (DictionaryHelperMethods.IsDictionary(a) && DictionaryHelperMethods.IsDictionary(b))
                //and they are dictionary equals
                return DictionaryHelperMethods.DictionaryEquals((IDictionary)a, (IDictionary)b);

            //if they are both arrays
            if (a is Array)
                return ArrayHelperMethods.ArrayEquals((Array)a, (Array)b);

            //they are not dictionaries or arrays
            return Equals(a, b);
        }
    }
}
