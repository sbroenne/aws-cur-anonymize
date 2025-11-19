# Test Data Files

This directory contains sample AWS Cost & Usage Report (CUR) files for testing.

## Files

### sample-legacy-csv.csv
- **Format**: Legacy CUR CSV with forward-slash column names
- **Schema Version**: Legacy CSV
- **Example columns**: `lineItem/UsageStartDate`, `bill/PayerAccountId`
- **Records**: 4 sample records covering EC2 and RDS usage

### sample-cur20.csv
- **Format**: CUR 2.0 (Data Exports) with snake_case column names
- **Schema Version**: CUR 2.0
- **Example columns**: `line_item_usage_start_date`, `bill_payer_account_id`
- **Records**: 5 sample records (same data as Legacy CSV for comparison)

### Note on Parquet Files

Legacy Parquet files are not included in the repository due to binary format and size. Tests that require Parquet files will:
1. Generate them dynamically from CSV data during test execution
2. Skip if DuckDB Parquet support is not available
3. Use real CUR data files if available in the `curdata/` directory at project root

## Usage in Tests

Integration tests use these files to validate:
- Schema version detection
- Column name normalization to Athena-compatible format
- Data processing and anonymization
- Query compatibility with AWS Athena SQL examples
- Error handling for different CUR formats

## Data Privacy

These files contain **synthetic test data only** - no real AWS account information or actual usage data. All account IDs and resource IDs are fictitious.
