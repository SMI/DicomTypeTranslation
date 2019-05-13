using System;

namespace DicomTypeTranslation.Elevation.Exceptions
{
    public class TagNavigationException : Exception
    {
        public TagNavigationException(string s):base(s)
        {
            
        }
    }
}