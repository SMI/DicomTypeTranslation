using System.Collections.Generic;
using FAnsi;
using YamlDotNet.Serialization;

namespace DicomTypeTranslation.TableCreation
{
    /// <summary>
    /// A collection of tables to create all at once.  This class primarily exists for serialization purposes and ETL design
    /// </summary>
    public class ImageTableTemplateCollection
    {
        /// <summary>
        /// The type of DBMS you intend to create your tables into
        /// </summary>
        public DatabaseType DatabaseType { get; set; }

        /// <summary>
        /// List of tables that should be created
        /// </summary>
        public List<ImageTableTemplate> Tables { get; set; }

        /// <summary>
        /// Creates a new empty collection
        /// </summary>
        public ImageTableTemplateCollection()
        {
            Tables = new List<ImageTableTemplate>();
        }

        /// <summary>
        /// Deserializes the <paramref name="yaml"/> into a new instance
        /// </summary>
        /// <param name="yaml"></param>
        /// <returns></returns>
        public static ImageTableTemplateCollection LoadFrom(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                                 .IgnoreUnmatchedProperties()
                                 .Build();

            return deserializer.Deserialize<ImageTableTemplateCollection>(yaml);
        }

        /// <summary>
        /// Serializes the class into a yaml string.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            Serializer serializer = new Serializer();

            return serializer.Serialize(this);
        }
    }
}
