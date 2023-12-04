using DicomTypeTranslation.Elevation.Serialization;
using System;

namespace DicomTypeTranslation.Elevation.Exceptions;

/// <summary>
/// Thrown when the xml provided to <see cref="TagElevationRequestCollection"/> is invalid
/// </summary>
public class MalformedTagElevationRequestCollectionXmlException : Exception
{
    /// <summary>
    /// Creates a new instance with the provided message
    /// </summary>
    /// <param name="message"></param>
    public MalformedTagElevationRequestCollectionXmlException(string message):base(message)
    {

    }

    /// <summary>
    /// Creatres a new instance with the provided message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="ex"></param>
    public MalformedTagElevationRequestCollectionXmlException(string message, Exception ex):base(message,ex)
    {

    }
}