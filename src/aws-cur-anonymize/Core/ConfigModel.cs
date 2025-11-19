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
                AnonymizeAccountIds = true,
                AnonymizeArns = true,
                HashTags = true
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
    /// Automatically anonymize AWS account ID columns.
    /// Default: true
    /// </summary>
    public bool AnonymizeAccountIds { get; set; } = true;

    /// <summary>
    /// Automatically anonymize ARNs in resource ID columns.
    /// Default: true
    /// </summary>
    public bool AnonymizeArns { get; set; } = true;

    /// <summary>
    /// Hash JSON tag columns (e.g., resource_tags in CUR 2.0).
    /// Default: true
    /// </summary>
    public bool HashTags { get; set; } = true;
}
