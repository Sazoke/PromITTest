using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ConsoleApp1;

public class DataAccess(string connectionString)
{
    private const string TMP_TABLE_NAME = "#tmpWords";

    private const string CREATE_TMP_TABLE_SQL = $"""
                                                 create table {TMP_TABLE_NAME} (
                                                     Word varchar(511),
                                                     Count int
                                                 );
                                                 """;

    private const string MERGE_SQL = $"""
                                      merge into Words with (HOLDLOCK) as tgt
                                      using {TMP_TABLE_NAME} as src
                                         on tgt.Word = src.Word
                                      when matched
                                         then
                                             update
                                             set tgt.Count = tgt.Count + src.Count
                                      when not matched by target
                                         then 
                                             insert (Word, Count)
                                             values (src.Word, src.Count);
                                      """;
    
    public async Task SaveStatsAsync(IEnumerable<WordStat> stats)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(CREATE_TMP_TABLE_SQL, transaction:transaction);
        using var copy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
        copy.DestinationTableName = TMP_TABLE_NAME;
        foreach (var name in typeof(WordStat).GetProperties().Select(p => p.Name))
            copy.ColumnMappings.Add(name, name);
        var dt = CreateDataTable(stats);
        await copy.WriteToServerAsync(dt);
        await connection.ExecuteAsync(MERGE_SQL, transaction:transaction);
        await transaction.CommitAsync();
    }


    private static DataTable CreateDataTable<T>(IEnumerable<T> values)
    {
        var table = new DataTable();

        // Get the generic type from the collection
        var type = values.GetType().GetGenericArguments()[0];

        // Add columns base on the type's properties
        foreach (var property in type.GetProperties())
        {
            /* It is necessary to evaluate whether each property is nullable or not.
             * This is because DataTables only support null values in the form of
             * DBNull.Value.
             */
            var propertyType = property.PropertyType;
            var computedType =
                // If the type is nullable
                propertyType.IsGenericType
                && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) 
                    // Get its underlying type
                    ? propertyType.GetGenericArguments()[0]
                    // If it isn't, get return the property type.
                    : propertyType;

            table.Columns.Add(new DataColumn(property.Name, computedType));
        }

        // Add rows into the DataTable based off of the values
        foreach (var value in values)
        {
            var row = table.NewRow();
            foreach (var property in value!.GetType().GetProperties())
            {
                // Create a container to hold the data in the value
                object? data = null;
                // If the property we are adding exists...
                if (row.Table.Columns.Contains(property.Name))
                    // Then get the value of that property
                    data = value.GetType().GetProperty(property.Name)!.GetValue(value, null)!;

                // If the value is null, convert the value to DBNull
                row[property.Name] = data ?? DBNull.Value;
            }
            table.Rows.Add(row);
        }

        return table;
    }
}