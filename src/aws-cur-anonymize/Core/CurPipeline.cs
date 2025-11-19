using DuckDB.NET.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace AwsCurAnonymize.Core;

public record CurProcessingStats(
    int OriginalColumnCount,
    int OutputColumnCount,
    int AnonymizedAccountColumns,
    int AnonymizedArnColumns,
    int HashedTagColumns,
    long InputRowCount,
    long OutputRowCount
);

public static class CurPipeline
{
    private static string NormalizePath(string p) => p.Replace("\\", "/");
    private static bool IsParquetPattern(string inputGlob)
        => inputGlob.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase) || inputGlob.Contains(".parquet", StringComparison.OrdinalIgnoreCase);
    private static string Q(string s) => s.Replace("'", "''");

    /// <summary>
    /// AWS ARN pattern: arn:partition:service:region:account-id:resource-type/resource-id
    /// </summary>
    private static readonly Regex ArnRegex = new Regex(
        @"^arn:([^:]+):([^:]*):([^:]*):(\d{12}):(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Anonymizes an AWS ARN by replacing the account ID with the anonymized version.
    /// </summary>
    private static string AnonymizeArn(string? arn, string salt)
    {
        if (string.IsNullOrWhiteSpace(arn))
        {
            return arn ?? string.Empty;
        }

        var match = ArnRegex.Match(arn);
        if (!match.Success)
        {
            // Not a valid ARN or doesn't contain account ID
            return arn;
        }

        var partition = match.Groups[1].Value;
        var service = match.Groups[2].Value;
        var region = match.Groups[3].Value;
        var accountId = match.Groups[4].Value;
        var resource = match.Groups[5].Value;

        // Generate anonymized account ID using same hash algorithm
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(salt + accountId)
        );
        var hashValue = Math.Abs(BitConverter.ToInt64(hash, 0));
        var anonAccountId = (hashValue % 100000000000).ToString().PadLeft(12, '0');

        return $"arn:{partition}:{service}:{region}:{anonAccountId}:{resource}";
    }

    /// <summary>
    /// Generates a SQL expression for ARN anonymization in DuckDB.
    /// Uses REGEXP_REPLACE to handle ARN transformation.
    /// </summary>
    private static string GetArnAnonymizationSql(string columnName, string salt)
    {
        // DuckDB SQL that extracts account ID from ARN and replaces it with hashed version
        return $@"CASE
            WHEN {columnName} IS NULL THEN NULL
            WHEN regexp_matches({columnName}, '^arn:[^:]+:[^:]*:[^:]*:\d{{12}}:.+$') THEN
                regexp_replace(
                    {columnName},
                    '^(arn:[^:]+:[^:]*:[^:]*:)(\d{{12}})(:.*)?$',
                    '\1' || lpad(CAST(abs(hash('{Q(salt)}' || '\2')) % 100000000000 AS VARCHAR), 12, '0') || '\3'
                )
            ELSE {columnName}
        END";

    }

    /// <summary>
    /// Gets all columns from a view/table and returns their names.
    /// </summary>
    private static async Task<List<string>> GetColumnsAsync(DuckDBCommand cmd, string tableName)
    {
        cmd.CommandText = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}' ORDER BY ordinal_position;";
        var columns = new List<string>();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
        }

        return columns;
    }

    /// <summary>
    /// Filters columns based on configuration include/exclude patterns.
    /// </summary>
    /// <summary>
    /// Filters columns based on configuration, using normalized column names for pattern matching.
    /// </summary>
    private static List<string> FilterColumns(List<string> allColumns, CurConfig config)
    {
        var filtered = new List<string>();

        foreach (var col in allColumns)
        {
            // Normalize the column name for pattern matching
            // CRITICAL: Must normalize BEFORE pattern matching because config patterns use normalized names
            // Example: "bill/PayerAccountId" → "bill_payer_account_id" to match "bill_*" pattern
            var normalizedName = AthenaColumnNormalizer.Normalize(col);

            if (ConfigLoader.ShouldIncludeColumn(normalizedName, config))
            {
                filtered.Add(col);
            }
        }

        return filtered;
    }

    /// <summary>
    /// Creates an Athena-compatible normalized view from raw CUR data.
    /// This view transforms column names to match AWS Athena's naming convention.
    /// Optionally filters columns based on configuration.
    /// </summary>
    private static async Task CreateNormalizedViewAsync(DuckDBCommand cmd, CurConfig? config = null, string sourceView = "cur")
    {
        // Get all column names from the source view
        var allColumns = await GetColumnsAsync(cmd, sourceView);

        // Apply filtering if config is provided
        var columns = config != null ? FilterColumns(allColumns, config) : allColumns;

        if (columns.Count == 0)
        {
            throw new InvalidOperationException("No columns remaining after filtering. Check your configuration.");
        }

        // Build SELECT statement with normalized column aliases
        var selectBuilder = new StringBuilder();
        selectBuilder.AppendLine("CREATE OR REPLACE VIEW cur_normalized AS");
        selectBuilder.AppendLine("SELECT");

        for (int i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            var alias = AthenaColumnNormalizer.CreateColumnAlias(col);
            selectBuilder.Append("  ");
            selectBuilder.Append(alias);
            if (i < columns.Count - 1)
                selectBuilder.AppendLine(",");
            else
                selectBuilder.AppendLine();
        }

        selectBuilder.Append($"FROM {sourceView};");

        cmd.CommandText = selectBuilder.ToString();
        cmd.ExecuteNonQuery();
    }



    public static async Task<CurProcessingStats> WriteDetailAsync(string inputGlob, string outputDir, string salt, string format, string? configPath = null, string outputFile = "cur_detail")
    {
        var inGlob = NormalizePath(inputGlob);
        var fmt = (format ?? "csv").ToLowerInvariant();
        var outPath = NormalizePath(Path.Combine(outputDir, fmt == "parquet" ? $"{outputFile}.parquet" : $"{outputFile}.csv"));

        // Load configuration
        var config = await ConfigLoader.LoadConfigAsync(configPath);

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        // Load raw data
        cmd.CommandText = IsParquetPattern(inGlob)
            ? $"CREATE VIEW cur AS SELECT * FROM read_parquet('{Q(inGlob)}');"
            : $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{Q(inGlob)}', HEADER=TRUE, ALL_VARCHAR=TRUE);";
        cmd.ExecuteNonQuery();

        // Create Athena-compatible normalized view with filtering
        await CreateNormalizedViewAsync(cmd, config, "cur");

        // Get all columns before filtering
        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'cur';";
        var originalColumnCount = Convert.ToInt32(cmd.ExecuteScalar());

        // Get normalized columns
        var normalizedColumns = await GetColumnsAsync(cmd, "cur_normalized");

        // Check which account ID columns exist
        var accountColumns = normalizedColumns
            .Where(col => col.Contains("account_id", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Check for ARN columns
        var arnColumns = normalizedColumns
            .Where(col => col.Contains("resource_id", StringComparison.OrdinalIgnoreCase) || col.Contains("_arn", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Check for tag columns
        var tagColumns = normalizedColumns
            .Where(col => col.Contains("_tags", StringComparison.OrdinalIgnoreCase) || col == "resource_tags")
            .ToList();

        // Count input rows from normalized view (before filtering)
        cmd.CommandText = "SELECT COUNT(*) FROM cur_normalized;";
        var inputRowCount = Convert.ToInt64(cmd.ExecuteScalar());

        // Build SELECT with anonymized/hashed columns based on config
        var selectBuilder = new StringBuilder();
        selectBuilder.AppendLine("CREATE OR REPLACE VIEW cur_masked AS");
        selectBuilder.AppendLine("SELECT");

        var processedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selectColumns = new List<string>();
        var anonymizedAccountCols = 0;
        var anonymizedArnCols = 0;
        var hashedTagCols = 0;

        foreach (var col in normalizedColumns)
        {
            // Apply smart defaults based on anonymization settings
            if (accountColumns.Contains(col) && config.Anonymization?.AnonymizeAccountIds == true)
            {
                selectColumns.Add($"  lpad(CAST(abs(hash('{Q(salt)}' || {col})) % 100000000000 AS VARCHAR), 12, '0') AS {col}");
                processedColumns.Add(col);
                anonymizedAccountCols++;
            }
            else if (arnColumns.Contains(col) && config.Anonymization?.AnonymizeArns == true)
            {
                selectColumns.Add($"  {GetArnAnonymizationSql(col, salt)} AS {col}");
                processedColumns.Add(col);
                anonymizedArnCols++;
            }
            else if (tagColumns.Contains(col) && config.Anonymization?.HashTags == true)
            {
                selectColumns.Add($"  md5(CAST({col} AS VARCHAR)) AS {col}");
                processedColumns.Add(col);
                hashedTagCols++;
            }
            else
            {
                selectColumns.Add($"  {col}");
                processedColumns.Add(col);
            }
        }

        if (selectColumns.Count == 0)
        {
            throw new InvalidOperationException("No columns to export after applying configuration.");
        }

        selectBuilder.Append(string.Join(",\n", selectColumns));
        selectBuilder.AppendLine();
        selectBuilder.Append("FROM cur_normalized");

        // Add WHERE clause for row filtering if configured
        if (config.RowFilters != null && config.RowFilters.Count > 0)
        {
            var whereConditions = new List<string>();

            foreach (var (columnName, allowedValues) in config.RowFilters)
            {
                if (allowedValues == null || allowedValues.Count == 0)
                    continue;

                // Check if the column exists in normalized view
                if (!normalizedColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Warning: Row filter column '{columnName}' not found in data. Skipping filter.");
                    continue;
                }

                // Build IN clause with quoted values
                var quotedValues = string.Join(", ", allowedValues.Select(v => $"'{Q(v)}'"));
                whereConditions.Add($"{columnName} IN ({quotedValues})");
            }

            if (whereConditions.Count > 0)
            {
                selectBuilder.AppendLine();
                selectBuilder.Append("WHERE ");
                selectBuilder.Append(string.Join(" AND ", whereConditions));
            }
        }

        selectBuilder.Append(";");

        cmd.CommandText = selectBuilder.ToString();
        cmd.ExecuteNonQuery();

        // Count output rows from masked view (after filtering)
        cmd.CommandText = "SELECT COUNT(*) FROM cur_masked;";
        var outputRowCount = Convert.ToInt64(cmd.ExecuteScalar());

        cmd.CommandText = fmt == "parquet"
            ? $"COPY (SELECT * FROM cur_masked) TO '{Q(outPath)}' (FORMAT PARQUET, COMPRESSION 'SNAPPY');"
            : $"COPY (SELECT * FROM cur_masked) TO '{Q(outPath)}' (FORMAT CSV, HEADER TRUE);";
        cmd.ExecuteNonQuery();

        return new CurProcessingStats(
            OriginalColumnCount: originalColumnCount,
            OutputColumnCount: selectColumns.Count,
            AnonymizedAccountColumns: anonymizedAccountCols,
            AnonymizedArnColumns: anonymizedArnCols,
            HashedTagColumns: hashedTagCols,
            InputRowCount: inputRowCount,
            OutputRowCount: outputRowCount
        );
    }

    /// <summary>
    /// Inspects input files and generates a configuration file with suggested actions.
    /// </summary>
    public static async Task GenerateConfigAsync(string inputGlob, string outputConfigPath)
    {
        var inGlob = NormalizePath(inputGlob);

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        // Load raw data (just to get column names)
        cmd.CommandText = IsParquetPattern(inGlob)
            ? $"CREATE VIEW cur AS SELECT * FROM read_parquet('{Q(inGlob)}') LIMIT 0;"
            : $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{Q(inGlob)}', HEADER=TRUE, ALL_VARCHAR=TRUE) LIMIT 0;";
        cmd.ExecuteNonQuery();

        // Create normalized view
        await CreateNormalizedViewAsync(cmd, null, "cur");

        // Get column names
        var columns = await GetColumnsAsync(cmd, "cur_normalized");

        // Generate config with smart defaults
        var config = ConfigLoader.GenerateConfigFromColumns(columns);

        // Save to file
        await ConfigLoader.SaveConfigAsync(outputConfigPath, config);
    }
}
