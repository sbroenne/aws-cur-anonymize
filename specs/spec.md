# aws-cur-anonymize – Full Project Specification

## ✅ GitHub Project Setup
- Repository Name: `aws-cur-anonymize`
- Description: Standalone .NET 10 tool to merge, anonymize, and summarize AWS Cost & Usage Reports (CUR) with CSV monthly summaries and optional full-detail exports.
- Visibility: Public
- Initial Files:
  - README.md (this spec)
  - .gitignore → VisualStudio template
  - License → MIT or Apache-2.0
- Folder Structure:
  ```
  src/aws-cur-anonymize/
  examples/
  .github/workflows/
  tests/aws-cur-anonymize.Tests/
  ```

---

## ✅ High-Level Goals
- Process local AWS CUR files (Parquet or CSV).
- Default output: monthly summary CSV.
- Optional: full-detail export, tag hashing, ARN rewriting.

---

## ✅ Technology Stack
- Language: C# (.NET 10)
- Packaging: Single-file executable per OS
- Libraries: System.CommandLine, DuckDB.NET.Data, Newtonsoft.Json, Serilog

---

## ✅ AWS CUR Schema Versions Support

AWS provides Cost and Usage Reports in multiple schema formats. This tool must support **all three versions**:

### 1. Legacy CUR CSV (Forward-Slash Columns)
- **Format**: CSV files with forward-slash column names
- **Example Columns**: `lineItem/UsageStartDate`, `bill/PayerAccountId`, `product/ProductName`
- **Characteristics**:
  - Column names use forward slashes as namespace separators
  - Column names are case-sensitive and quoted in SQL
  - Tag and cost category names are **not normalized** (spaces and special characters preserved)
  - Most common format for existing CUR exports
- **Detection**: CSV files are assumed to be this format by default

### 2. Legacy CUR Parquet (Underscore Columns)
- **Format**: Parquet files with normalized underscore column names
- **Example Columns**: `lineitem_usagestartdate`, `bill_payeraccountid`, `product_productname`
- **Characteristics**:
  - Column names use underscores instead of forward slashes
  - Column names are lowercase and normalized
  - Tag and cost category names **are normalized** (spaces and special characters removed)
  - Better performance for large datasets
- **Detection**: Files with `.parquet` extension

### 3. CUR 2.0 (Data Exports Schema)
- **Format**: New schema via AWS Data Exports service
- **Example Columns**: `line_item_usage_start_date`, `bill_payer_account_id`, `bill_payer_account_name`
- **Characteristics**:
  - **Fixed schema** - consistent columns regardless of AWS usage
  - Snake_case column naming convention
  - **Nested columns** with key-value pairs (resource_tags, cost_category, product, discount)
  - Two exclusive columns: `bill_payer_account_name`, `line_item_usage_account_name`
  - Reduced data sparsity compared to legacy CUR
- **Recommended**: AWS's new preferred format (see [migration guide](https://docs.aws.amazon.com/cur/latest/userguide/dataexports-migrate.html))

### Schema Compatibility Strategy

The tool handles schema differences using:

1. **Automatic Schema Detection**: Sniff CSV headers to detect schema version
   - Read first line of CSV to examine column names
   - Detect forward-slash columns → Legacy CSV format
   - Detect snake_case with `line_item_` prefix → CUR 2.0 format
   - Parquet files examined via DuckDB metadata query

2. **Type Safety**: Force `ALL_VARCHAR=TRUE` when reading CSV to prevent DuckDB type inference errors
   - AWS CUR columns can contain mixed data types (e.g., `product/maxIopsvolume` = "500 - based on 1 MiB I/O size" or numeric)
   - Type casting happens explicitly in SQL queries only for columns we need

3. **Athena-Compatible Schema Normalization**:
   - **Goal**: Create views in DuckDB that match Athena's column naming convention
   - **Athena Transformation Rules** (from AWS docs):
     - Underscore added before uppercase letters
     - Uppercase letters → lowercase
     - Non-alphanumeric characters → underscore
     - Duplicate underscores removed
     - Leading/trailing underscores removed
   - **Examples**:
     - `lineItem/UsageStartDate` → `line_item_usage_start_date`
     - `bill/PayerAccountId` → `bill_payer_account_id`
     - `product/ProductName` → `product_product_name`

4. **Normalized View Strategy**:
   ```sql
   -- After loading raw CSV/Parquet, create normalized view
   CREATE VIEW cur_normalized AS
   SELECT 
     "lineItem/UsageStartDate" AS line_item_usage_start_date,
     "bill/PayerAccountId" AS bill_payer_account_id,
     "product/ProductName" AS product_product_name,
     "lineItem/UnblendedCost" AS line_item_unblended_cost,
     -- ... all other columns normalized
   FROM cur_raw;
   ```

5. **AWS Athena Sample Queries Compatibility**:
   The normalized view enables running standard AWS Athena queries from documentation:
   ```sql
   -- Example: Year-to-date costs by service (from AWS docs)
   SELECT line_item_product_code,
          sum(line_item_blended_cost) AS cost,
          month
   FROM cur_normalized
   WHERE year='2024'
   GROUP BY line_item_product_code, month
   HAVING sum(line_item_blended_cost) > 0
   ORDER BY line_item_product_code;
   ```

6. **Integration Test Queries** (based on AWS documentation examples):
   - Monthly cost by service: `SELECT line_item_product_code, sum(line_item_unblended_cost) GROUP BY 1`
   - Top 10 resources by cost: `SELECT line_item_resource_id, sum(line_item_unblended_cost) ORDER BY 2 DESC LIMIT 10`
   - Daily usage trends: `SELECT DATE(line_item_usage_start_date), sum(line_item_usage_amount) GROUP BY 1`
   - Account-level aggregation: `SELECT bill_payer_account_id, line_item_usage_account_id, sum(line_item_unblended_cost)`

7. **Account ID Column Mapping**:
   - Legacy CSV/Parquet: `bill/PayerAccountId` → `bill_payer_account_id`
   - CUR 2.0: Already `bill_payer_account_id`

### References
- [AWS CUR Data Dictionary](https://docs.aws.amazon.com/cur/latest/userguide/data-dictionary.html)
- [CUR 2.0 Migration Guide](https://docs.aws.amazon.com/cur/latest/userguide/dataexports-migrate.html)
- [Column Attribute Reference (CSV)](https://docs.aws.amazon.com/cur/latest/userguide/samples/Column_Attribute_Service.zip)

---

## ✅ Core Features
- CLI commands: `init`, `run`, `validate`, `sample`
- Default behavior: monthly summary CSV grouped by bill_month, anonymized account_id, product.
- Sensitive fields anonymized: account IDs, ARNs, resource IDs, tags.

---

## ✅ Implementation Plan
### Phase 1 – MVP
- Scaffold .NET solution
- Implement CLI
- Monthly summary pipeline using DuckDB
- Full-detail export (CSV/Parquet)
- Config loader and validator
- CI and Release workflows

### Phase 2 – Enhancements
- Tag hashing and ARN rewriting
- Custom group-by options
- Additional output formats

---

## ✅ Success Criteria
- CLI runs on Windows/Linux/macOS
- Processes local CUR files
- Produces cur_summary_monthly.csv
- Deterministic anonymization with same salt
- Single-file executables published
- Unit tests and coverage ≥ 80%
- **Integration tests validate Athena-compatible queries work in DuckDB**

---

## ✅ Integration Test Queries (Athena Compatibility)

To ensure the normalized DuckDB schema matches AWS Athena's format, integration tests should run these AWS-documented sample queries:

### Test 1: Year-to-Date Costs by Service
```sql
SELECT line_item_product_code,
       sum(line_item_blended_cost) AS cost,
       month
FROM cur_normalized
WHERE year='2024'
GROUP BY line_item_product_code, month
HAVING sum(line_item_blended_cost) > 0
ORDER BY line_item_product_code;
```
**Expected**: Grouped costs per service per month

### Test 2: Monthly Unblended Costs by Account
```sql
SELECT bill_payer_account_id,
       line_item_usage_account_id,
       DATE_TRUNC('month', CAST(line_item_usage_start_date AS TIMESTAMP)) AS month,
       SUM(CAST(line_item_unblended_cost AS DOUBLE)) AS total_cost
FROM cur_normalized
GROUP BY 1, 2, 3
ORDER BY 3, 4 DESC;
```
**Expected**: Account-level cost aggregation by month

### Test 3: Top 10 Most Expensive Resources
```sql
SELECT line_item_resource_id,
       line_item_product_code,
       SUM(CAST(line_item_unblended_cost AS DOUBLE)) AS total_cost
FROM cur_normalized
WHERE line_item_resource_id IS NOT NULL
  AND line_item_resource_id != ''
GROUP BY 1, 2
ORDER BY 3 DESC
LIMIT 10;
```
**Expected**: Resource-level cost breakdown

### Test 4: Daily Usage Trends
```sql
SELECT DATE(CAST(line_item_usage_start_date AS TIMESTAMP)) AS usage_date,
       line_item_product_code,
       SUM(CAST(line_item_usage_amount AS DOUBLE)) AS total_usage
FROM cur_normalized
GROUP BY 1, 2
ORDER BY 1, 3 DESC;
```
**Expected**: Time-series usage data

### Test 5: Verify Column Name Normalization
```sql
-- Test that all expected Athena columns exist
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'cur_normalized'
  AND column_name IN (
    'line_item_usage_start_date',
    'line_item_usage_end_date',
    'bill_payer_account_id',
    'line_item_usage_account_id',
    'line_item_product_code',
    'line_item_usage_type',
    'line_item_unblended_cost',
    'line_item_blended_cost',
    'product_product_name'
  );
```
**Expected**: All 9 columns present

---

## ✅ Example CLI Usage
```powershell
aws-cur-anonymize run --input "cur/2025-09/*.parquet" --output out/ --salt "MY-STRONG-SALT"
aws-cur-anonymize run --input "cur/2025-09/*.parquet" --output out/ --detail true
aws-cur-anonymize run --input "cur/2025-09/*.parquet" --output out/ --detail true --format parquet
```

---

## ✅ Starter Code Blocks
### Program.cs
```csharp
using System.CommandLine;
using AwsCurAnonymize.Cli;

var root = Commands.BuildRoot();
return await root.InvokeAsync(args);
```

### Cli/Commands.cs
```csharp
using System.CommandLine;
using AwsCurAnonymize.Core;

namespace AwsCurAnonymize.Cli;

public static class Commands
{
    public static RootCommand BuildRoot()
    {
        var root = new RootCommand("AWS CUR anonymizer (CSV monthly summary default)");
        root.AddCommand(Run());
        return root;
    }

    private static Command Run()
    {
        var cmd = new Command("run", "Merge, anonymize, and summarize CUR");
        var input = new Option<string>("--input") { IsRequired = true };
        var output = new Option<string>("--output") { IsRequired = true };
        var detail = new Option<bool>("--detail", () => false);
        var format = new Option<string>("--format", () => "csv");
        var saltOpt = new Option<string>("--salt", "Optional salt (else use CUR_ANON_SALT env var)");

        cmd.AddOptions(input, output, detail, format, saltOpt);
        cmd.SetHandler(async (string i, string o, bool d, string f, string? s) =>
        {
            var salt = !string.IsNullOrWhiteSpace(s) ? s : Environment.GetEnvironmentVariable("CUR_ANON_SALT");
            if (string.IsNullOrWhiteSpace(salt))
            {
                Console.Error.WriteLine("Missing salt. Set --salt or env CUR_ANON_SALT.");
                Environment.Exit(2);
            }
            Directory.CreateDirectory(o);
            if (!d)
                await CurPipeline.WriteMonthlySummaryCsvAsync(i, o, salt!);
            else
                await CurPipeline.WriteDetailAsync(i, o, salt!, f);
            Console.WriteLine($"Done → {o}");
        }, input, output, detail, format, saltOpt);
        return cmd;
    }
}
```

### Core/CurPipeline.cs
```csharp
using DuckDB.NET.Data;

namespace AwsCurAnonymize.Core;

public static class CurPipeline
{
    private static string NormalizePath(string p) => p.Replace("\\", "/");
    private static bool IsParquetPattern(string inputGlob)
        => inputGlob.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase) || inputGlob.Contains(".parquet", StringComparison.OrdinalIgnoreCase);
    private static string Q(string s) => s.Replace("'", "''");

    public static async Task WriteMonthlySummaryCsvAsync(string inputGlob, string outputDir, string salt)
    {
        var inGlob = NormalizePath(inputGlob);
        var outCsv = NormalizePath(Path.Combine(outputDir, "cur_summary_monthly.csv"));

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        cmd.CommandText = IsParquetPattern(inGlob)
            ? $"CREATE VIEW cur AS SELECT * FROM read_parquet('{Q(inGlob)}');"
            : $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{Q(inGlob)}', HEADER=TRUE);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = $@"
CREATE OR REPLACE VIEW cur_t AS
SELECT
  date_trunc('month', to_timestamp(COALESCE(""lineItem/UsageStartDate"", lineItem_UsageStartDate))) AS bill_month,
  lpad(CAST(abs(hash('{Q(salt)}' || COALESCE(""identity/PayerAccountId"", identity_PayerAccountId::VARCHAR))) % 100000000000 AS VARCHAR), 12, '0') AS account_id,
  COALESCE(""product/ProductName"", product_ProductName, ""lineItem/UsageType"", lineItem_UsageType) AS product,
  CAST(COALESCE(""lineItem/UnblendedCost"", lineItem_UnblendedCost) AS DOUBLE) AS unblended_cost,
  CAST(COALESCE(""lineItem/UsageAmount"", lineItem_UsageAmount) AS DOUBLE) AS usage_amount
FROM cur;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = $@"
COPY (
  SELECT bill_month, account_id, product,
         SUM(unblended_cost) AS total_cost,
         SUM(usage_amount) AS total_usage
  FROM cur_t
  GROUP BY 1,2,3
  ORDER BY 1,2,3
) TO '{Q(outCsv)}' (FORMAT CSV, HEADER TRUE);";
        cmd.ExecuteNonQuery();
    }

    public static async Task WriteDetailAsync(string inputGlob, string outputDir, string salt, string format)
    {
        var inGlob = NormalizePath(inputGlob);
        var fmt = (format ?? "csv").ToLowerInvariant();
        var outPath = NormalizePath(Path.Combine(outputDir, fmt == "parquet" ? "cur_detail.parquet" : "cur_detail.csv"));

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        cmd.CommandText = IsParquetPattern(inGlob)
            ? $"CREATE VIEW cur AS SELECT * FROM read_parquet('{Q(inGlob)}');"
            : $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{Q(inGlob)}', HEADER=TRUE);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = $@"
CREATE OR REPLACE VIEW cur_masked AS
SELECT
  *,
  lpad(CAST(abs(hash('{Q(salt)}' || COALESCE(""identity/PayerAccountId"", identity_PayerAccountId::VARCHAR))) % 100000000000 AS VARCHAR), 12, '0') AS account_id_anon
FROM cur;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = fmt == "parquet"
            ? $"COPY (SELECT * EXCLUDE (\"identity/PayerAccountId\", identity_PayerAccountId) FROM cur_masked) TO '{Q(outPath)}' (FORMAT PARQUET, COMPRESSION 'SNAPPY');"
            : $"COPY (SELECT * EXCLUDE (\"identity/PayerAccountId\", identity_PayerAccountId) FROM cur_masked) TO '{Q(outPath)}' (FORMAT CSV, HEADER TRUE);";
        cmd.ExecuteNonQuery();
    }
}
```

---

## ✅ Future Enhancements
