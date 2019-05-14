using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DicomTypeTranslation.Elevation.Exceptions;

namespace DicomTypeTranslation.Elevation.Serialization
{

    /// <summary>
    /// Handles serialization/deserialization of TagElevationRequests from xml
    /// </summary>
    public class TagElevationRequestCollection
    {
        /// <summary>
        /// All <see cref="TagElevationRequest"/> that are part of the current collection
        /// </summary>
        public List<TagElevationRequest> Requests = new List<TagElevationRequest>();

        /// <summary>
        /// Creates a new collection instance by deserializing the <paramref name="xml"/>
        /// </summary>
        /// <param name="xml"></param>
        public TagElevationRequestCollection(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            var root = doc["TagElevationRequestCollection"];
            
            if (root == null)
                throw new MalformedTagElevationRequestCollectionXmlException("No root tag TagElevationRequestCollection");

            foreach (XmlElement requestXml in root.ChildNodes)
            {
                var toAdd = new TagElevationRequest(requestXml);
                Requests.Add(toAdd);
            }
        }
    }
}
