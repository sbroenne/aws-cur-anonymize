# Changelog

All notable changes to the aws-cur-anonymize project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- AWS Athena schema compatibility with automatic column name normalization
- Support for all three CUR schema versions (Legacy CSV, Legacy Parquet, CUR 2.0)
- `AthenaColumnNormalizer` to transform CUR columns to Athena-compatible format
- Dynamic `cur_normalized` view creation with standardized column names
- Integration tests validating AWS Athena sample queries
- Documentation for Athena-compatible SQL queries

### Changed

- `CurPipeline` now creates normalized views following AWS transformation rules
- Column names automatically converted to snake_case format (e.g., `line_item_usage_start_date`)
- Improved handling of scientific notation and negative costs in test assertions

## [1.0.0] - 2025-10-25

### Added

- Initial release
- Command-line interface with `run` command
- Monthly summary CSV generation from AWS CUR files
- Full-detail export support (CSV and Parquet formats)
- Deterministic anonymization of AWS account IDs using salt-based hashing
- Support for both Parquet and CSV input formats
- DuckDB-based data processing for performance
- Environment variable support for salt configuration
- Cross-platform support (Windows, Linux, macOS)
- Comprehensive documentation and examples

[Unreleased]: https://github.com/yourusername/aws-cur-anonymize/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/yourusername/aws-cur-anonymize/releases/tag/v1.0.0
