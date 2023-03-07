using System.Xml;
using DicomTypeTranslation.Elevation.Exceptions;
using FellowOakDicom;

namespace DicomTypeTranslation.Elevation.Serialization;

/// <summary>
/// Describes a request to identify a <see cref="DicomTag"/> in a nested <see cref="DicomSequence"/> and return the value(s) matching for storage
/// in a database/output <see cref="ColumnName"/>
/// </summary>
public class TagElevationRequest
{
    /// <summary>
    /// The output column (in a database or DataTable etc) which should be populated with the found value(s)
    /// </summary>
    public string ColumnName { get; set; }
        
    /// <summary>
    /// The path of <see cref="DicomSequence"/> and subsequences to travel and the leaf (non sequence) node to fetch e.g. "ContentSequence->TextValue"
    /// </summary>
    public string ElevationPathway { get; set; }

    /// <summary>
    /// Optional relative pathway once reaching leaf(s) to decide whether to return the leaf.  Only valid when <see cref="ConditionalRegex"/> is set.
    /// </summary>
    public string ConditionalPathway { get; set; }

    /// <summary>
    /// The regex pattern to run on adjacent nodes (to the leaf being evaluated) to decide whether to return the leaf.  Once a leaf is found by walking the
    /// <see cref="ElevationPathway"/> this regex is run on the adjacent tags (according to the <see cref="ConditionalPathway"/>).
    /// </summary>
    public string ConditionalRegex { get; set; }

    /// <summary>
    /// Implementation class which will handle resolving this request
    /// </summary>
    public TagElevator Elevator { get; private set; }

    /// <summary>
    /// Creates a new instance by deserializing the given xml
    /// </summary>
    /// <param name="element"></param>
    public TagElevationRequest(XmlNode element)
    {
        if(element.Name != "TagElevationRequest")
            throw new MalformedTagElevationRequestCollectionXmlException("Expected xml element name to be TagElevationRequest");

        ColumnName = element["ColumnName"].InnerText;
        ElevationPathway = element["ElevationPathway"].InnerText;


        var conditional = element["Conditional"];

        if (conditional != null)
        {
            ConditionalPathway = conditional["ConditionalPathway"]?.InnerText;
            ConditionalRegex = conditional["ConditionalRegex"]?.InnerText;
        }

        Elevator = new TagElevator(this);
    }
}