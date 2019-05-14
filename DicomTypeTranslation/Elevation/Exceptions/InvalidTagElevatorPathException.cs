using System;

namespace DicomTypeTranslation.Elevation.Exceptions
{
    /// <summary>
    /// Thrown when a <see cref="TagElevator"/> path is mistyped or otherwise illegal
    /// </summary>
    public class InvalidTagElevatorPathException : Exception
    {
        /// <summary>
        /// Creates a new instance with the provided message
        /// </summary>
        /// <param name="message"></param>
        public InvalidTagElevatorPathException(string message): base(message)
        {
            
        }
        /// <summary>
        /// Creatres a new instance with the provided message and inner exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public InvalidTagElevatorPathException(string message,Exception ex): base(message,ex)
        {
            
        }
    }
}