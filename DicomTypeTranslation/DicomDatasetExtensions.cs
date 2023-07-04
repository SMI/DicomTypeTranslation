
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FellowOakDicom;


namespace DicomTypeTranslation;

/// <summary>
/// Extension methods for <see cref="DicomDataset"/>
/// </summary>
public static class DicomDatasetExtensions
{
    /// <summary>
    /// Converts the <paramref name="dataset"/> into a new row <paramref name="inTable"/>.  Columns will automatically be created
    /// for all top level tags.
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="inTable"></param>
    /// <returns></returns>
    public static DataRow ToRow(this DicomDataset dataset, DataTable inTable)
    {
        var row = inTable.Rows.Add();

        //for each item in the dataset
        foreach (var i in dataset)
            AddColumnValue(dataset, row, i);

        return row;
    }

    /// <summary>
    ///  Converts the <paramref name="dataset"/> into a new row <paramref name="inTable"/>.  Columns will automatically be created
    ///  for all top level tags that are in <paramref name="onlyTheseTags"/>.
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="inTable"></param>
    /// <param name="onlyTheseTags"></param>
    /// <returns></returns>
    public static DataRow ToRow(this DicomDataset dataset, DataTable inTable, ICollection<string> onlyTheseTags)
    {
        var row = inTable.Rows.Add();

        //for each item in the dataset
        foreach (var i in dataset.Where(i=> onlyTheseTags.Contains(DicomTypeTranslaterReader.GetColumnNameForTag(i.Tag, false))))
            AddColumnValue(dataset, row, i);

        return row;
    }

    /// <summary>
    ///  Converts the <paramref name="dataset"/> into a new row <paramref name="inTable"/>.  Columns will automatically be created
    ///  for all top level tags that are selected by <paramref name="inTable"/>.
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="inTable"></param>
    /// <param name="filterTags">Function returning true if the item should be added to the table</param>
    /// <returns></returns>
    public static DataRow ToRow(this DicomDataset dataset, DataTable inTable, Func<DicomItem, bool> filterTags)
    {
        var row = inTable.Rows.Add();

        //for each item in the dataset
        foreach (var i in dataset.Where(filterTags))
            AddColumnValue(dataset, row, i);

        return row;

    }
    private static void AddColumnValue(DicomDataset dataset, DataRow row, DicomItem i)
    {
        //get the column name for the tag
        var name = DicomTypeTranslaterReader.GetColumnNameForTag(i.Tag, false);

        //if we don't have it in our table yet
        if (!row.Table.Columns.Contains(name))
        {
            //add it with the correct hard Type
            var type = DicomTypeTranslater.GetNaturalTypeForVr(i.ValueRepresentation, i.Tag.DictionaryEntry.ValueMultiplicity);
            row.Table.Columns.Add(name, type.CSharpType);
        }

        //populate row value
        row[name] = DicomTypeTranslaterReader.GetCSharpValue(dataset, i);
    }
}