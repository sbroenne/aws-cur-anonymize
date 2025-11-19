# aws-cur-anonymize

[![.NET Build and Test](https://github.com/yourusername/aws-cur-anonymize/actions/workflows/dotnet.yml/badge.svg)](https://github.com/yourusername/aws-cur-anonymize/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download)

Standalone .NET 10 tool to anonymize AWS Cost & Usage Reports (CUR) with secure account ID and ARN anonymization.

## Features

- 📊 **Individual File Processing**: Processes each CUR file separately with unique output filenames
- 📈 **Processing Statistics**: Displays before/after column counts and anonymization metrics
- 🔒 **Secure Anonymization**: Deterministic salt-based hashing of AWS account IDs and ARNs
- 🎛️ **Required Config Files**: Fine-grained control over column filtering and anonymization
- 🏷️ **Tag Hashing**: Hash CUR 2.0 JSON tags to protect sensitive values
- 🔗 **ARN Rewriting**: Intelligently anonymizes account IDs within AWS ARNs
- 🎯 **Column Filtering**: Include/exclude patterns with proper normalized column name matching
- 🚀 **High Performance**: DuckDB-based processing for fast data operations
- 📁 **Flexible Input**: Supports both Parquet and CSV CUR file formats
- 💾 **Multiple Output Formats**: Export as CSV or Parquet
- 🔧 **Cross-Platform**: Runs on Windows (x64, ARM64), Linux, and macOS
- ✨ **AWS Athena Compatible**: Automatic column normalization matches Athena table schema

## Table of Contents

- [Getting AWS CUR Data](#getting-aws-cur-data)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage](#usage)
- [Configuration](#configuration)
- [Examples](#examples)
- [Building from Source](#building-from-source)
- [Contributing](#contributing)
- [Security](#security)
- [License](#license)

## Getting AWS CUR Data

Before using this tool, you need to export Cost & Usage Report data from AWS. Here's how:

### Step 1: Enable Cost & Usage Reports in AWS

1. Sign in to the [AWS Billing Console](https://console.aws.amazon.com/billing/)
2. Navigate to **Cost & Usage Reports** in the left menu
3. Click **Create report**

**Report configuration:**
- **Report name**: Choose a descriptive name (e.g., `my-cur-report`)
- **Time granularity**: **Monthly** (recommended for anonymization and cost comparison)
- **Report versioning**: **Overwrite existing report**
- **Enable report data integration**: Check **Amazon Athena** (optional - creates Parquet files)

**S3 bucket configuration:**
- **S3 bucket**: Select or create an S3 bucket to store CUR files
- **Report path prefix**: Optional (e.g., `cur-data/`)
- **Compression**: **GZIP** (creates CSV files, recommended for compatibility)

4. Click **Next** → **Review and Complete**

### Step 2: Wait for Data Generation

- AWS generates CUR files **once per day** (usually takes 8-24 hours for first report)
- Files appear in your S3 bucket at: `s3://your-bucket/prefix/report-name/`
- Each month's data is in a separate folder

### Step 3: Download CUR Files from S3

**Using AWS CLI:**

```bash
# List available CUR files
aws s3 ls s3://your-bucket/cur-data/my-cur-report/ --recursive

# Download a specific month
aws s3 sync s3://your-bucket/cur-data/my-cur-report/20250101-20250201/ ./curdata/

# Or download specific files
aws s3 cp s3://your-bucket/cur-data/my-cur-report/20250101-20250201/my-report.csv ./curdata/
```

**Using AWS Console:**
1. Go to [S3 Console](https://s3.console.aws.amazon.com/)
2. Navigate to your CUR bucket
3. Browse to the report folder
4. Download `.csv` or `.parquet` files

### Step 4: Run the Anonymizer

```powershell
# Process downloaded CUR files
aws-cur-anonymize "curdata/*.csv" --output anonymized/

# Or if you downloaded Parquet files
aws-cur-anonymize "curdata/*.parquet" --output anonymized/
```

### CUR Format Support

This tool supports all AWS CUR formats:
- **Legacy CSV** (forward-slash columns): `lineItem/UsageStartDate`
- **Legacy Parquet** (underscore columns): `lineitem_usagestartdate`
- **CUR 2.0** (Data Exports, snake_case): `line_item_usage_start_date`

The tool automatically detects the format and normalizes column names to Athena-compatible format.

### Need Help?

- **CUR not generating?** Check IAM permissions - your user needs S3 and Billing access
- **Want smaller files?** Enable Athena integration for Parquet format (faster processing but less compatible)
- **More detail needed?** Monthly granularity is best for anonymization; Daily/Hourly creates large files with excessive detail

## Installation

### Download Pre-built Binary

Download the latest release for your platform from the [Releases](https://github.com/yourusername/aws-cur-anonymize/releases) page:

- **Windows**: `aws-cur-anonymize-win-x64.zip`
- **Linux**: `aws-cur-anonymize-linux-x64.tar.gz`
- **macOS**: `aws-cur-anonymize-osx-arm64.tar.gz`

Extract and run the executable directly.

## Quick Start

**IMPORTANT**: This tool requires a `cur-config.yaml` configuration file. A template is included with the release.

**Simplest usage** - process files in a directory:

```powershell
# Ensure cur-config.yaml is in current directory
aws-cur-anonymize run --input "curdata/*.csv" --output out/
```

This processes each CSV file individually, creating separate output files like:
- `out/sample-100k.csv` (derived from `curdata/sample-100k.csv`)
- `out/monthly-costs-00001.csv` (derived from `curdata/monthly-costs-00001.csv`)

**Processing Statistics** are displayed for each file:
```
Processing: curdata/sample-100k.csv
Columns: 245 → 42
Anonymized: 2 account, 1 ARN
Output: out/sample-100k.csv
```

**Custom output directory**:

```powershell
aws-cur-anonymize run --input "cur/*.csv" --output results/
```

**Config file behavior**:
- Tool looks for `cur-config.yaml` in current directory by default
- Use `--config path/to/config.yaml` to specify a different location
- Config file is **required** - tool will fail if not found
- Edit config to control column filtering and anonymization

### Optional: Using a Custom Salt

For deterministic anonymization (same accounts map to same anonymized IDs across runs):

```powershell
# Provide your own salt
aws-cur-anonymize "cur/*.csv" --salt "your-strong-random-salt"

# Or set environment variable
$env:CUR_ANON_SALT = "your-strong-random-salt"
aws-cur-anonymize
```

## Usage

Process and anonymize AWS CUR files.

```bash
aws-cur-anonymize run --input [pattern] --output [directory] [options]
```

#### Required Arguments

| Argument | Description | Example |
|----------|-------------|---------|------
| `--input` | Input glob pattern (e.g., `"*.csv"`, `"data/*.parquet"`) | `--input "curdata/*.csv"` |
| `--output` | Output directory where results will be saved | `--output out/` |

#### Optional Arguments

| Option | Description | Default |
|--------|-------------|---------|
| `--salt` | Salt value for anonymization. Can also use `CUR_ANON_SALT` env var | Auto-generated (random) |
| `--format` | Output format (`csv` or `parquet`) | `csv` |
| `--detail` | Export full detail data (always true currently) | `true` |
| `--config` | YAML config file path | `cur-config.yaml` in current directory |

### Examples

#### 1. Basic Usage - Process CSV Files

```powershell
aws-cur-anonymize run --input "curdata/*.csv" --output out/
```

**Output**: Individual files in `out/` directory:
- `out/sample-100k.csv`
- `out/monthly-costs-00001.csv`

**Statistics displayed**:
```
Processing: curdata/sample-100k.csv
Columns: 245 → 42
Anonymized: 2 account, 1 ARN
Output: out/sample-100k.csv
```

#### 2. Custom Output Directory

```powershell
aws-cur-anonymize run --input "cur/*.csv" --output results/
```

**Output**: Individual anonymized files in `results/` directory

#### 3. Using a Custom Configuration File

Edit the included `cur-config.yaml` file to customize:

```powershell
# Edit cur-config.yaml to customize column filtering and anonymization
notepad cur-config.yaml

# Tool automatically uses cur-config.yaml if present
aws-cur-anonymize run --input "cur/*.csv" --output out/

# Or specify a custom config
aws-cur-anonymize run --input "cur/*.csv" --output out/ --config my-config.yaml
```

#### 4. Processing Parquet Input Files

```powershell
aws-cur-anonymize run --input "cur/2025-09/*.parquet" --output out/
```

**Output**: Individual CSV files in `out/` (one per input Parquet file)

#### 5. Using Environment Variable for Salt

```powershell
# Set salt once for deterministic anonymization
$env:CUR_ANON_SALT = "your-secret-salt-value"

# Run without --salt parameter
aws-cur-anonymize run --input "cur/*.csv" --output out/
```

#### 6. Parquet Output Format

```powershell
aws-cur-anonymize run --input "cur/2025-09/*.csv" --output out/ --format parquet
```

**Output**: Individual Parquet files in `out/` (e.g., `out/monthly-costs.parquet`)

## Configuration Files

### Required Configuration File

**IMPORTANT**: This tool requires a `cur-config.yaml` configuration file to run.

**The tool includes a `cur-config.yaml` template alongside the executable** in published releases. You must:
- **Have a config file**: Tool will fail with an error if `cur-config.yaml` is not found
- **Customize as needed**: Edit `cur-config.yaml` to adjust column filtering and anonymization
- **Override location**: Use `--config path/to/custom.yaml` to specify a different config file

The tool automatically looks for `cur-config.yaml` in the current working directory.

**Quick setup:**
```powershell
# Download and extract the release
# The cur-config.yaml file is already included!

# Edit the config (optional - default works for most cases)
notepad cur-config.yaml

# Run for a directory (config file required)
aws-cur-anonymize run --input "cur/*.csv" --output out/

# Or use a custom config
aws-cur-anonymize run --input "cur/*.csv" --output out/ --config my-config.yaml
```

### Overview

Configuration files allow fine-grained control over which columns to keep, anonymize, and which rows to filter. All configuration is done in YAML format with inline comments for documentation.

### Config Structure

**See [`cur-config.yaml`](cur-config.yaml)** for a complete example with inline documentation explaining:
- Pattern-based column filtering
- Row filtering for usage-only data
- Anonymization settings
- Comments for each field

### Anonymization Settings

Control how sensitive data is handled. These settings automatically detect columns by name pattern:

| Setting | Description | Auto-Detects |
|---------|-------------|--------------|
| `anonymize_account_ids` | Anonymize AWS account ID columns | Columns containing `account_id` |
| `anonymize_arns` | Anonymize ARN columns | Columns containing `resource_id` or `_arn` |
| `hash_tags` | Hash tag columns | Columns containing `_tags` or named `resource_tags` |

**Default**: All three are enabled (`true`) when no config is provided.

**Example transformations**:
- `anonymize_account_ids: true` → `123456789012` becomes `000087654321`
- `anonymize_arns: true` → `arn:aws:...:123456...` becomes `arn:aws:...:00008...`
- `hash_tags: false` → Tags remain readable as-is

### Include/Exclude Patterns

**Purpose**: Bulk column filtering using wildcards. CUR files have 200+ columns - patterns let you control groups of columns at once without listing each individually.

**CRITICAL**: Patterns are matched against **normalized column names** (using underscores). All CUR column names are normalized to Athena-compatible format before pattern matching:
- `bill/PayerAccountId` → `bill_payer_account_id`
- `lineItem/UsageStartDate` → `line_item_usage_start_date`

**`include_patterns`** (Whitelist):
- Only columns matching these patterns are included
- If empty or omitted, all columns are included by default
- Use when you only want specific column groups
- Example: `"bill_*"` matches `bill_payer_account_id`, `bill_billing_period_start_date`

**`exclude_patterns`** (Blacklist):
- Columns matching these patterns are removed
- Applied after include_patterns
- Use to remove sensitive columns like identity data
- Example: `"identity_*"` removes all identity columns

**Processing Order**:
1. Start with all columns from your CUR file
2. **Normalize all column names** (forward-slash or mixed-case → underscore snake_case)
3. If `include_patterns` is set, keep only matching columns
4. Then remove any columns matching `exclude_patterns`
5. Finally, apply specific column actions from `columns` section

**Glob Wildcards**:
- `*` matches any characters
- `?` matches single character
- Case-insensitive matching

**Real-World Examples**:

1. **Remove all sensitive identity columns**:
   - `exclude_patterns: ["identity_*"]`
   - Result: Columns like `identity_line_item_id`, `identity_time_interval` are removed

2. **Only keep billing and cost columns**:
   - `include_patterns: ["line_item_*", "bill_*", "product_*"]`
   - Result: Only matching columns kept; `reservation_*`, `identity_*`, `pricing_*` removed

3. **Keep cost data but remove all ID columns**:
   - `include_patterns: ["line_item_*", "bill_*"]`
   - `exclude_patterns: ["*_id", "*_arn"]`
   - Result: Keeps cost columns but removes resource IDs and ARNs

**When to use**:
- **No patterns**: Use all columns (default behavior)
- **Only exclude_patterns**: Remove sensitive columns while keeping everything else
- **Only include_patterns**: Narrow down to specific column families
- **Both**: Precise control - include broad groups, then exclude specific items

### Row Filters

**Purpose**: Filter entire rows based on column values. This removes rows that don't match specified criteria, useful for excluding fees, taxes, credits, and other non-usage line items.

**`row_filters`**: Dictionary where keys are column names and values are lists of allowed values.
- Only rows where the column value matches one of the allowed values are kept
- Multiple filters are combined with AND logic
- Case-sensitive matching

**Example - Keep only actual usage rows**:
```yaml
row_filters:
  line_item_line_item_type:
    - Usage
    - SavingsPlanCoveredUsage
    - DiscountedUsage
```

This filters out:
- `RIFee` - Reserved Instance recurring fees
- `Fee` - Other AWS fees
- `Tax` - Tax charges
- `Credit` - Credits and refunds
- `SavingsPlanNegation` - Savings Plan adjustments
- `SavingsPlanRecurringFee` - SP recurring fees

**When to use**:
- **Cloud pricing comparison**: Keep only usage rows (default behavior)
- **Cost analysis**: Include all row types to see complete billing picture
- **Service filtering**: Filter by `line_item_product_code` to analyze specific services

### Complete Example Configuration

See [`cur-config.yaml`](cur-config.yaml) for a complete, documented example showing:
- Pattern-based column filtering for AWS→Azure/Google pricing comparison
- Row filtering to exclude fees, taxes, and credits
- Anonymization settings with inline explanations

## Advanced Usage

### Custom Anonymization

**Default behavior**: When no config is provided:
```yaml
anonymization:
  anonymize_account_ids: true
  anonymize_arns: true
  hash_tags: false
```

## Configuration

### Environment Variables

| Variable | Description |
|----------|-------------|
| `CUR_ANON_SALT` | Optional: Default salt value for deterministic anonymization. If not set, a random salt is auto-generated for each run. |

## Output Files

### Individual File Processing

Each input file is processed separately and produces its own output file with the same base name:

**Input**: `curdata/sample-100k.csv`  
**Output**: `out/sample-100k.csv`

**Input**: `curdata/monthly-costs-00001.parquet`  
**Output**: `out/monthly-costs-00001.csv` (or `.parquet` if `--format parquet`)

### Output Content

Each output file contains:
- **Filtered columns**: Only columns matching config include/exclude patterns
- **Anonymized account IDs**: Replaced with 12-digit hashed values
- **Anonymized ARNs**: Rebuilt with anonymized account IDs (preserves resource structure)
- **Hashed tags**: Tag values hashed to protect sensitive data (if enabled)
- **Normalized column names**: All columns use Athena-compatible snake_case format

### Processing Statistics

Displayed for each file:
```
Columns: 245 → 42          # Original columns → After filtering
Anonymized: 2 account, 1 ARN  # Number of columns anonymized
```

## Anonymization Details

### Account ID Anonymization

The tool uses deterministic SHA-256 hashing:

1. Combines the salt with the original account ID
2. Computes SHA-256 hash
3. Converts to a 12-digit number (matching AWS account ID format)

**Properties**: 
- Same salt + same account ID = same anonymized ID (consistent)
- Different salt = different anonymized ID (can't reverse)
- No way to recover original account ID without the salt

### ARN Anonymization

AWS ARNs contain account IDs that must be anonymized. The tool:

1. Parses ARN structure: `arn:partition:service:region:account-id:resource`
2. Replaces the account ID with the anonymized version (using same hash as above)
3. Preserves all other ARN components (partition, service, region, resource)

**Example**:
```
Original: arn:aws:ec2:us-east-1:123456789012:instance/i-1234567890abcdef0
Anonymized: arn:aws:ec2:us-east-1:000087654321:instance/i-1234567890abcdef0
```

### Tag Hashing

CUR 2.0 includes a `resource_tags` column with JSON tag data. The tool can hash this column to:
- Protect sensitive tag values
- Preserve uniqueness for analysis
- Make data shareable without exposing tag details

Enable/disable via config: `"anonymization": { "hash_tags": true }`

## AWS Athena Schema Compatibility

The tool automatically normalizes CUR column names to match AWS Athena table schemas. This ensures you can use standard AWS SQL queries with your anonymized data.

### Column Name Normalization

AWS CUR files come in different formats:

- **Legacy CSV**: `lineItem/UsageStartDate`, `bill/PayerAccountId`
- **Legacy Parquet**: `lineitem_usagestartdate`, `bill_payeraccountid`
- **CUR 2.0**: `line_item_usage_start_date`, `bill_payer_account_id`

All formats are automatically normalized to **Athena-compatible names** following AWS transformation rules:

```text
lineItem/UsageStartDate → line_item_usage_start_date
bill/PayerAccountId → bill_payer_account_id
product/ProductName → product_product_name
pricing/publicOnDemandRate → pricing_public_on_demand_rate
```

### Using Athena Queries

You can use standard AWS Athena queries directly with the anonymized data:

```sql
-- Monthly costs by service
SELECT line_item_product_code,
       DATE_TRUNC('month', CAST(line_item_usage_start_date AS TIMESTAMP)) AS month,
       SUM(CAST(line_item_unblended_cost AS DOUBLE)) AS total_cost
FROM cur_normalized
WHERE line_item_product_code IS NOT NULL
GROUP BY line_item_product_code, DATE_TRUNC('month', CAST(line_item_usage_start_date AS TIMESTAMP))
ORDER BY line_item_product_code, month;

-- Top 10 resources by cost
SELECT line_item_resource_id,
       line_item_product_code,
       SUM(CAST(line_item_unblended_cost AS DOUBLE)) AS total_cost
FROM cur_normalized
WHERE line_item_resource_id IS NOT NULL
GROUP BY line_item_resource_id, line_item_product_code
ORDER BY total_cost DESC
LIMIT 10;

-- Account-level costs
SELECT bill_payer_account_id,
       line_item_usage_account_id,
       SUM(CAST(line_item_unblended_cost AS DOUBLE)) AS total_cost
FROM cur_normalized
GROUP BY bill_payer_account_id, line_item_usage_account_id
ORDER BY total_cost DESC;
```

**Note**: The tool uses DuckDB internally, which has similar SQL syntax to Athena. Query the `cur_normalized` view for Athena-compatible column names.

## Building from Source

See [DEVELOPER_GUIDE.md](docs/DEVELOPER_GUIDE.md) for complete build instructions, development workflow, and testing guidelines.


## Advanced: Custom Salt Values

The salt value controls anonymization behavior:

**Without Salt (Auto-generated)**:
- Tool generates a random salt for each run
- Same account ID → **different** anonymized ID each time
- Use for one-time data sharing (no need to track accounts across runs)

**With Salt (Deterministic)**:
- Same salt + same account ID → **same** anonymized ID every time
- Use when you need to track account costs over multiple CUR exports
- Example: Monthly cost trends for specific (anonymized) accounts

**Best Practices**:
- **Keep it secret**: Store in environment variables or secret managers
- **Use a strong value**: Generate cryptographically random strings
- **Be consistent**: Use the same salt for related reports
- **Length**: At least 16 characters recommended

Generate a strong salt:

```bash
# Linux/macOS
openssl rand -base64 32

# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Standards

- **Coding Style**: Follow C# best practices
- **Documentation**: XML comments for public APIs
- **Testing**: Comprehensive unit and integration tests
- **Code Coverage**: Maintain or improve coverage

## Security

This project takes security seriously:

- Regular security scanning via GitHub Advanced Security
- Automated dependency updates
- Secure handling of sensitive data

See [SECURITY.md](SECURITY.md) for:
- Reporting vulnerabilities
- Security best practices
- Supported versions

## Roadmap

Future enhancements planned:

- Additional output formats (JSON, Excel)
- Incremental data processing
- Performance optimizations for very large datasets

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [DuckDB](https://duckdb.org/) for high-performance data processing
- Powered by [.NET 10](https://dotnet.microsoft.com/)
- CLI powered by [Spectre.Console](https://spectreconsole.net/)

## Support

- 📖 [Documentation](https://github.com/yourusername/aws-cur-anonymize/wiki)
- 🐛 [Report Issues](https://github.com/yourusername/aws-cur-anonymize/issues)
- 💬 [Discussions](https://github.com/yourusername/aws-cur-anonymize/discussions)

**Note**: This tool is not affiliated with, endorsed by, or connected to Amazon Web Services (AWS). It is an independent tool for processing AWS Cost & Usage Reports.
