using DicomTypeTranslation.Elevation.Serialization;
using System;

namespace DicomTypeTranslation.Elevation.Exceptions
{
    /// <summary>
    /// Thrown when there is an error traversing an <see cref="TagElevationRequest.ElevationPathway"/> e.g. if the 'leaf' node in the 
    /// path is a sequence tag (not allowed).
    /// </summary>
    public class TagNavigationException : Exception
    {
        /// <summary>
        /// Creates a new instance with the provided message
        /// </summary>
        /// <param name="message"></param>
        public TagNavigationException(string message):base(message)
        {
            
        }

        /// <summary>
        /// Creatres a new instance with the provided message and inner exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public TagNavigationException(string message,Exception ex):base(message,ex)
        {
            
        }
    }
}