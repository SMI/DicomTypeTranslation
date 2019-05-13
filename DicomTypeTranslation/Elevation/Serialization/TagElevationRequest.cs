using System.Xml;
using DicomTypeTranslation.Elevation.Exceptions;

namespace DicomTypeTranslation.Elevation.Serialization
{
    public class TagElevationRequest
    {
        public string ColumnName { get; set; }
        public string ElevationPathway { get; set; }
        public string ConditionalPathway { get; set; }
        public string ConditionalRegex { get; set; }

        public TagElevator Elevator { get; private set; }

        public TagElevationRequest(XmlElement element)
        {
            if(element.Name != "TagElevationRequest")
                throw new MalformedTagElevationRequestCollectionXmlException("Expected xml element name to be TagElevationRequest");

            ColumnName = element["ColumnName"].InnerText;
            ElevationPathway = element["ElevationPathway"].InnerText;


            var conditional = element["Conditional"];

            if (conditional != null)
            {
                ConditionalPathway = conditional["ConditionalPathway"].InnerText;
                ConditionalRegex = conditional["ConditionalRegex"].InnerText;
            }

            Elevator = new TagElevator(this);
        }
    }
}
