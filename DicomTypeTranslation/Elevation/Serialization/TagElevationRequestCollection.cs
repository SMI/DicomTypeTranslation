using System.Collections.Generic;
using System.Linq;
using System.Xml;
using DicomTypeTranslation.Elevation.Exceptions;

namespace DicomTypeTranslation.Elevation.Serialization;

/// <summary>
/// Handles serialization/deserialization of TagElevationRequests from xml
/// </summary>
public class TagElevationRequestCollection
{
    /// <summary>
    /// All <see cref="TagElevationRequest"/> that are part of the current collection
    /// </summary>
    public readonly List<TagElevationRequest> Requests = new();

    /// <summary>
    /// Creates a new collection instance by deserializing the <paramref name="xml"/>
    /// </summary>
    /// <param name="xml"></param>
    public TagElevationRequestCollection(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var root = doc["TagElevationRequestCollection"] ?? throw new MalformedTagElevationRequestCollectionXmlException("No root tag TagElevationRequestCollection");
        foreach (var n in root.ChildNodes.Cast<XmlNode>().Where(n=>n is not XmlComment))
        {
            var requestXml = (XmlElement)n;
            var toAdd = new TagElevationRequest(requestXml);
            Requests.Add(toAdd);
        }
    }
}