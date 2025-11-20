namespace AwsCurAnonymize.Core;

/// <summary>
/// Configuration model for CUR anonymization with pattern-based column filtering.
/// When no config file is provided, defaults are used (automatic account ID anonymization).
/// </summary>
public class CurConfig
{
    /// <summary>
    /// Optional comment field for config documentation.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Column include patterns (glob-style). If specified, only matching columns are processed.
    /// Example: ["line_item_*", "bill_*"]
    /// </summary>
    public List<string>? IncludePatterns { get; set; }

    /// <summary>
    /// Column exclude patterns (glob-style). Matching columns are removed from output.
    /// Example: ["*_tags", "identity_*"]
    /// </summary>
    public List<string>? ExcludePatterns { get; set; }

    /// <summary>
    /// Anonymization settings.
    /// </summary>
    public AnonymizationSettings? Anonymization { get; set; }

    /// <summary>
    /// Row filters - only include rows where column values match specified values.
    /// Key: column name, Value: list of allowed values.
    /// Example: { "line_item_line_item_type": ["Usage", "SavingsPlanCoveredUsage"] }
    /// </summary>
    public Dictionary<string, List<string>>? RowFilters { get; set; }

    /// <summary>
    /// Creates a default configuration with automatic account ID anonymization.
    /// </summary>
    public static CurConfig CreateDefault()
    {
        return new CurConfig
        {
            Comment = "Auto-generated default configuration",
            Anonymization = new AnonymizationSettings
            {
                AnonymizationPatterns = new List<string>
                {
                    "payer_account_id",
                    "linked_account_id",
                    "*_account_id"
                }
            },
            RowFilters = new Dictionary<string, List<string>>
            {
                { "record_type", new List<string> { "LineItem", "PayerLineItem", "LinkedLineItem" } }
            }
        };
    }
}

/// <summary>
/// Anonymization settings for sensitive data.
/// </summary>
public class AnonymizationSettings
{
    /// <summary>
    /// Column patterns to anonymize using MD5 hash.
    /// Supports glob patterns: *, ?
    /// Example: ["payer_account_id", "linked_account_id", "*_account_name"]
    /// Default: account ID columns
    /// </summary>
    public List<string>? AnonymizationPatterns { get; set; }
}
