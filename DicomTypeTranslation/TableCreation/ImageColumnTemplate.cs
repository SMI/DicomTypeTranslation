using Dicom;
using FAnsi.Discovery;

namespace DicomTypeTranslation.TableCreation
{
    /// <summary>
    /// Describes a column to be created in a relational database based on a dicom tag
    /// </summary>
    public class ImageColumnTemplate
    {
        /// <summary>
        /// The name of the column to create, this MUST be a <see cref="DicomTag"/> name (see <see cref="DicomTypeTranslaterReader.GetColumnNameForTag"/>)
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// True to create a column in the database which allows nulls
        /// </summary>
        public bool AllowNulls { get; set; }

        /// <summary>
        /// True to make this column part of the primary key in the table created.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Create a new column based on the <see cref="DatabaseColumnRequest"/> (See <see cref="DicomTypeTranslater.GetNaturalTypeForVr(DicomVR, DicomVM)"/>)
        /// </summary>
        /// <param name="databaseColumnRequest"></param>
        public ImageColumnTemplate(DatabaseColumnRequest databaseColumnRequest)
        {
            ColumnName = databaseColumnRequest.ColumnName;
            AllowNulls = databaseColumnRequest.AllowNulls;
            IsPrimaryKey = databaseColumnRequest.IsPrimaryKey;
        }

        /// <summary>
        /// Create a new empty instance (used for deserialization).
        /// </summary>
        public ImageColumnTemplate()
        {

        }

        /// <summary>
        /// Create a new column based on the <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag"></param>
        public ImageColumnTemplate(DicomTag tag)
        {
            ColumnName = DicomTypeTranslaterReader.GetColumnNameForTag(tag,false);            
        }
    }
}
