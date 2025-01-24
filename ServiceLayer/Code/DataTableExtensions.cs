using System;
using System.Data;

public static class DataTableExtensions
{
    /// <summary>
    /// Removes spaces from all column names in the DataTable.
    /// </summary>
    /// <param name="dataTable">The DataTable to process.</param>
    public static void RemoveSpacesFromColumnNames(this DataTable dataTable)
    {
        if (dataTable == null)
        {
            throw new ArgumentNullException(nameof(dataTable), "DataTable cannot be null.");
        }

        foreach (DataColumn column in dataTable.Columns)
        {
            column.ColumnName = column.ColumnName.Replace(" ", "");
        }
    }
}
