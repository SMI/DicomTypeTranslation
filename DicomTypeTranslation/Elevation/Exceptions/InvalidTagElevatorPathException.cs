using System;

namespace DicomTypeTranslation.Elevation.Exceptions
{
    public class InvalidTagElevatorPathException : Exception
    {
        public InvalidTagElevatorPathException(string message): base(message)
        {
            
        }
    }
}