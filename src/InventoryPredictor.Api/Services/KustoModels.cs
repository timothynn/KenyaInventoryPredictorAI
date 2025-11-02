namespace InventoryPredictor.Api.Services;

public class KustoQueryResult
{
    public List<KustoTable> Tables { get; set; } = new();
}

public class KustoTable
{
    public string TableName { get; set; } = string.Empty;
    public List<KustoColumn> Columns { get; set; } = new();
    public List<List<object>> Rows { get; set; } = new();
}

public class KustoColumn
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}
