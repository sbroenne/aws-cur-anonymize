namespace AwsCurAnonymize.Core;

/// <summary>
/// AWS Cost and Usage Report schema versions
/// </summary>
public enum CurSchemaVersion
{
    /// <summary>Legacy CUR CSV format with forward-slash column names (e.g., "lineItem/UsageStartDate")</summary>
    LegacyCsv,

    /// <summary>Legacy CUR Parquet format with underscore column names (e.g., lineitem_usagestartdate)</summary>
    LegacyParquet,

    /// <summary>CUR 2.0 format with snake_case column names (e.g., line_item_usage_start_date)</summary>
    Cur20
}

/// <summary>
/// Column name mappings for different CUR schema versions
/// </summary>
public class CurSchemaMapping
{
    public CurSchemaVersion Version { get; }
    public string UsageStartDate { get; }
    public string PayerAccountId { get; }
    public string ProductName { get; }
    public string UsageType { get; }
    public string UnblendedCost { get; }
    public string UsageAmount { get; }

    private CurSchemaMapping(CurSchemaVersion version, string usageStartDate, string payerAccountId,
        string productName, string usageType, string unblendedCost, string usageAmount)
    {
        Version = version;
        UsageStartDate = usageStartDate;
        PayerAccountId = payerAccountId;
        ProductName = productName;
        UsageType = usageType;
        UnblendedCost = unblendedCost;
        UsageAmount = usageAmount;
    }

    public static CurSchemaMapping ForVersion(CurSchemaVersion version) => version switch
    {
        CurSchemaVersion.LegacyCsv => new CurSchemaMapping(
            version,
            usageStartDate: "\"lineItem/UsageStartDate\"",
            payerAccountId: "\"bill/PayerAccountId\"",
            productName: "\"product/ProductName\"",
            usageType: "\"lineItem/UsageType\"",
            unblendedCost: "\"lineItem/UnblendedCost\"",
            usageAmount: "\"lineItem/UsageAmount\""
        ),
        CurSchemaVersion.LegacyParquet => new CurSchemaMapping(
            version,
            usageStartDate: "lineitem_usagestartdate",
            payerAccountId: "bill_payeraccountid",
            productName: "product_productname",
            usageType: "lineitem_usagetype",
            unblendedCost: "lineitem_unblendedcost",
            usageAmount: "lineitem_usageamount"
        ),
        CurSchemaVersion.Cur20 => new CurSchemaMapping(
            version,
            usageStartDate: "line_item_usage_start_date",
            payerAccountId: "bill_payer_account_id",
            productName: "product_product_name",
            usageType: "line_item_usage_type",
            unblendedCost: "line_item_unblended_cost",
            usageAmount: "line_item_usage_amount"
        ),
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };

    /// <summary>
    /// Detect CUR schema version from CSV header line
    /// </summary>
    public static CurSchemaVersion DetectFromCsvHeader(string headerLine)
    {
        // Legacy CSV: Contains forward-slash columns like "lineItem/UsageStartDate"
        if (headerLine.Contains("lineItem/UsageStartDate", StringComparison.OrdinalIgnoreCase))
            return CurSchemaVersion.LegacyCsv;

        // CUR 2.0: Contains snake_case columns like "line_item_usage_start_date"
        if (headerLine.Contains("line_item_usage_start_date", StringComparison.OrdinalIgnoreCase))
            return CurSchemaVersion.Cur20;

        // Default to Legacy CSV (most common)
        return CurSchemaVersion.LegacyCsv;
    }

    /// <summary>
    /// Detect CUR schema version from a CSV file path
    /// </summary>
    public static async Task<CurSchemaVersion> DetectFromCsvFileAsync(string csvPath)
    {
        if (!File.Exists(csvPath))
            throw new FileNotFoundException($"CSV file not found: {csvPath}");

        // Read just the header line
        using var reader = new StreamReader(csvPath);
        var headerLine = await reader.ReadLineAsync();

        if (string.IsNullOrEmpty(headerLine))
            throw new InvalidDataException($"CSV file is empty or has no header: {csvPath}");

        return DetectFromCsvHeader(headerLine);
    }

    /// <summary>
    /// Detect schema from glob pattern by examining first matching file
    /// </summary>
    public static async Task<CurSchemaVersion> DetectFromGlobAsync(string globPattern)
    {
        // For parquet, assume LegacyParquet format
        if (globPattern.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase) ||
            globPattern.Contains(".parquet", StringComparison.OrdinalIgnoreCase))
        {
            return CurSchemaVersion.LegacyParquet;
        }

        // For CSV, find first matching file and sniff it
        var directory = Path.GetDirectoryName(globPattern);
        var pattern = Path.GetFileName(globPattern);

        if (string.IsNullOrEmpty(directory))
            directory = Directory.GetCurrentDirectory();

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        var files = Directory.GetFiles(directory, pattern);
        if (files.Length == 0)
            throw new FileNotFoundException($"No files match pattern: {globPattern}");

        // Examine first file
        return await DetectFromCsvFileAsync(files[0]);
    }
}
