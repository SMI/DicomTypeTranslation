using System;
using System.Linq;
using FellowOakDicom;
using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;
using FAnsi.Discovery.TypeTranslation;
using TypeGuesser;

namespace DicomTypeTranslation.TableCreation;

/// <summary>
/// Handles creating SQL scripts and relational database tables based on <see cref="ImageTableTemplate"/> (collection of which DicomTags you want stored).
/// </summary>
public class ImagingTableCreation
{
    private readonly IQuerySyntaxHelper _querySyntaxHelper;

    /// <summary>
    /// Special column name for storing the file system path to the image being described.
    /// </summary>
    public const string RelativeFileArchiveURI = "RelativeFileArchiveURI";

    /// <summary>
    /// Creates a request for a column named <see cref="RelativeFileArchiveURI"/> this is a special column that will always be 512 in lenght and is used
    /// to store a file path to the image being described.
    /// </summary>
    /// <param name="allowNulls"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static DatabaseColumnRequest GetRelativeFileArchiveURIColumn(bool allowNulls, bool pk)
    {
        return new DatabaseColumnRequest(RelativeFileArchiveURI, new DatabaseTypeRequest(typeof(string), 512),
                allowNulls)
            { IsPrimaryKey = pk };
    }

    /// <summary>
    /// Creates a new instance of the creation engine ready to create tables in the provided <see cref="IQuerySyntaxHelper"/>
    /// </summary>
    /// <param name="querySyntaxHelper"></param>
    public ImagingTableCreation(IQuerySyntaxHelper querySyntaxHelper)
    {
        _querySyntaxHelper = querySyntaxHelper;
    }


    /// <summary>
    /// Creates the <paramref name="tableTemplate"/> in the database location <paramref name="expectedTable"/>.
    /// </summary>
    /// <param name="expectedTable"></param>
    /// <param name="tableTemplate"></param>
    public void CreateTable(DiscoveredTable expectedTable, ImageTableTemplate tableTemplate)
    {
        expectedTable.Database.CreateTable(expectedTable.GetRuntimeName(), tableTemplate.GetColumns(expectedTable.Database.Server.DatabaseType));

        if (!expectedTable.Exists())
            throw new Exception("Table did not exist after issuing create statement!");
    }

    /// <summary>
    /// Returns the table creation script to create a new table of the given <paramref name="tablename"/> in the supplied <paramref name="expectedDatabase"/>
    /// that matches the <paramref name="tableTemplate"/>
    /// </summary>
    /// <param name="expectedDatabase"></param>
    /// <param name="tablename"></param>
    /// <param name="tableTemplate"></param>
    /// <param name="schema">Only applies to DBMS which support schemas (e.g. dbo)</param>
    /// <returns></returns>
    public string GetCreateTableSql(DiscoveredDatabase expectedDatabase,string tablename, ImageTableTemplate tableTemplate,string schema=null)
    {
        return expectedDatabase.Helper.GetCreateTableSql(expectedDatabase,tablename, tableTemplate.GetColumns(expectedDatabase.Server.DatabaseType),null,false,schema);
    }

    /// <summary>
    /// Returns a <see cref="DatabaseColumnRequest"/> that describes the provided <paramref name="col"/>.  If the column is a
    /// <see cref="DicomTag"/> then <see cref="ImageColumnTemplate.Type"/> can be null
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public DatabaseColumnRequest GetColumnDefinition(ImageColumnTemplate col)
    {
        if (col.Type != null)
            return new DatabaseColumnRequest(col.ColumnName, col.Type, col.AllowNulls) { IsPrimaryKey = col.IsPrimaryKey };

        return GetColumnDefinition(col.ColumnName,col.AllowNulls,col.IsPrimaryKey);
    }

    /// <summary>
    /// Returns a <see cref="DatabaseColumnRequest"/> that describes the provided dicom <paramref name="tag"/> (See <see cref="GetDataTypeForTag"/>).  Also supports
    /// <see cref="RelativeFileArchiveURI"/> as <paramref name="tag"/>.
    /// 
    /// <para>This overload does NOT handle arbitrary (non DicomTag) columns, use <see cref="GetColumnDefinition(ImageColumnTemplate)"/> instead</para>
    /// 
    /// </summary>
    /// <param name="tag">The name of a dicom tag or <see cref="RelativeFileArchiveURI"/></param>
    /// <param name="allowNulls"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    public DatabaseColumnRequest GetColumnDefinition(string tag, bool allowNulls = true, bool pk = false)
    {
        if (tag == RelativeFileArchiveURI)
            return GetRelativeFileArchiveURIColumn(allowNulls, pk);

        if (tag == "MessageGuid")
            return new DatabaseColumnRequest("MessageGuid", new DatabaseTypeRequest(typeof(string), int.MaxValue), allowNulls) { IsPrimaryKey = pk };

        var tt = _querySyntaxHelper.TypeTranslater;

        return new DatabaseColumnRequest(tag, GetDataTypeForTag(tag, tt), allowNulls) { IsPrimaryKey = pk };
    }

    /// <summary>
    /// Returns the DBMS specific datatype for storing a dicom tag of the supplied <paramref name="keyword"/> (must be a dicom tag name).
    /// </summary>
    /// <param name="keyword"></param>
    /// <param name="tt"></param>
    /// <returns></returns>
    public static string GetDataTypeForTag(string keyword, ITypeTranslater tt)
    {
        var tag = DicomDictionary.Default.FirstOrDefault(t => t.Keyword == keyword) ?? throw new NotSupportedException(
                $"Keyword '{keyword}' is not a valid Dicom Tag and no DatabaseTypeRequest was provided");
        var type = DicomTypeTranslater.GetNaturalTypeForVr(tag.ValueRepresentations, tag.ValueMultiplicity);
        return tt.GetSQLDBTypeForCSharpType(type);
    }

}