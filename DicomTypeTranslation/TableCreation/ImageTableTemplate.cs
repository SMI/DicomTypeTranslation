using System.Linq;
using Dicom;
using FAnsi;
using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;

namespace DicomTypeTranslation.TableCreation
{
    /// <summary>
    /// Describes a table schema you intend to create for storing Dicom image metadata
    /// </summary>
    public class ImageTableTemplate
    {
        /// <summary>
        /// The table name that should be created in the database when deploying the template
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The columns that should be created in the database when deploying the template
        /// </summary>
        public ImageColumnTemplate[] Columns { get; set; }

        /// <summary>
        /// Creates a new instance ready to deploy into the DBMS <paramref name="databaseType"/> (See <see cref="ImagingTableCreation"/> for actually creating
        /// the SQL / running the creation).
        /// </summary>
        /// <param name="databaseType"></param>
        /// <returns></returns>
        public DatabaseColumnRequest[] GetColumns(FAnsi.DatabaseType databaseType)
        {
            var tableCreation = new ImagingTableCreation(new QuerySyntaxHelperFactory().Create(databaseType));
            return Columns.Select(c => tableCreation.GetColumnDefinition(c)).ToArray();
        }
    }
}
