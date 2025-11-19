# Test Specification for aws-cur-anonymize

## Test Categories

### 1. CLI Command Tests

#### 1.1 Basic CLI Structure
- [x] `BuildRoot_ReturnsValidRootCommand` - Root command exists
- [x] `BuildRoot_ContainsRunCommand` - Run command is present
- [x] `RunCommand_HasRequiredOptions` - Required options (--input, --output)
- [x] `RunCommand_HasOptionalOptions` - Optional options (--detail, --format, --salt)

#### 1.2 Command Execution
- [x] `RunCommand_WithValidArguments_ExecutesSuccessfully` - Happy path execution
- [ ] `RunCommand_WithMissingInput_ShowsError` - Missing --input option
- [ ] `RunCommand_WithMissingOutput_ShowsError` - Missing --output option
- [ ] `RunCommand_WithMissingSalt_ShowsError` - Missing --salt and env var
- [ ] `RunCommand_WithEnvironmentSalt_UsesEnvVariable` - CUR_ANON_SALT env var
- [ ] `RunCommand_WithBothSalts_PrefersCliOption` - CLI --salt overrides env var

#### 1.3 Input Validation
- [ ] `RunCommand_WithNonExistentFile_ShowsError` - File not found
- [ ] `RunCommand_WithInvalidGlob_ShowsError` - Invalid glob pattern
- [ ] `RunCommand_WithEmptyGlob_ShowsError` - No files match pattern
- [ ] `RunCommand_WithInvalidFormat_ShowsError` - Invalid --format value (not csv/parquet)

---

### 2. File Input Processing Tests

#### 2.1 CSV Input
- [x] `ProcessCsv_WithRealData_CreatesValidSummary` - Large CSV file (608K rows)
- [ ] `ProcessCsv_WithLegacyFormat_DetectsForwardSlashColumns` - Legacy CSV schema
- [ ] `ProcessCsv_WithEmptyFile_HandlesGracefully` - Empty CSV file
- [ ] `ProcessCsv_WithHeaderOnly_HandlesGracefully` - CSV with only header row
- [ ] `ProcessCsv_WithSingleRow_ProcessesCorrectly` - CSV with 1 data row
- [ ] `ProcessCsv_WithMissingColumns_ShowsError` - Required columns missing
- [ ] `ProcessCsv_WithMalformedData_HandlesGracefully` - Corrupt CSV data

#### 2.2 Parquet Input
- [ ] `ProcessParquet_WithValidFile_CreatesValidSummary` - Parquet input processing
- [ ] `ProcessParquet_WithLegacyFormat_DetectsUnderscoreColumns` - Legacy Parquet schema
- [ ] `ProcessParquet_WithEmptyFile_HandlesGracefully` - Empty Parquet file
- [ ] `ProcessParquet_WithMalformedFile_ShowsError` - Corrupt Parquet file

#### 2.3 Mixed Input
- [ ] `ProcessMixed_CsvAndParquet_ProcessesBoth` - Glob matching both formats
- [ ] `ProcessMultiple_SameFormat_MergesCorrectly` - Multiple CSV/Parquet files

#### 2.4 Schema Detection
- [ ] `DetectSchema_LegacyCsv_ReturnsForwardSlash` - Detects lineItem/UsageStartDate
- [ ] `DetectSchema_LegacyParquet_ReturnsUnderscore` - Detects lineitem_usagestartdate
- [ ] `DetectSchema_Cur20_ReturnsSnakeCase` - Detects line_item_usage_start_date
- [ ] `DetectSchema_UnknownFormat_ShowsError` - Unrecognized column names

---

### 3. Data Anonymization Tests

#### 3.1 Account ID Anonymization
- [x] `Anonymize_SameSalt_ProducesDeterministicResults` - Same input + salt = same output
- [x] `Anonymize_DifferentSalt_ProducesDifferentResults` - Different salts differ
- [ ] `Anonymize_EmptyAccountId_HandlesGracefully` - Null/empty account IDs
- [ ] `Anonymize_InvalidAccountId_HandlesGracefully` - Non-numeric account IDs
- [ ] `Anonymize_AccountIdFormat_Returns12Digits` - Output is 12-digit string

#### 3.2 Salt Handling
- [ ] `Salt_WithEmptyString_ShowsError` - Empty salt rejected
- [ ] `Salt_WithWhitespace_ShowsError` - Whitespace-only salt rejected
- [ ] `Salt_WithSpecialChars_AcceptsAll` - Special characters allowed
- [ ] `Salt_WithUnicode_AcceptsAll` - Unicode characters allowed
- [ ] `Salt_MinimumLength_Enforced` - Minimum salt length check

---

### 4. Monthly Summary Output Tests

#### 4.1 CSV Output Structure
- [x] `MonthlySummary_WithRealData_CreatesValidCsv` - Output file created
- [ ] `MonthlySummary_CsvHeaders_MatchesExpected` - Correct header row
- [ ] `MonthlySummary_CsvFormat_ValidRFC4180` - Valid CSV formatting
- [ ] `MonthlySummary_DateFormat_YYYYMMDD` - bill_month in YYYY-MM-DD
- [ ] `MonthlySummary_AccountIdFormat_12Digits` - account_id is 12 digits

#### 4.2 Aggregation Logic
- [x] `MonthlySummary_AggregatesDataCorrectly` - Sums costs and usage
- [ ] `MonthlySummary_GroupsByMonthAccountProduct` - Correct grouping
- [ ] `MonthlySummary_HandleNullCosts_CorrectlyAggregates` - Null costs as 0
- [ ] `MonthlySummary_HandleNegativeCosts_IncludesCredits` - AWS credits/refunds
- [ ] `MonthlySummary_HandleZeroCosts_IncludesInOutput` - Zero-cost items

#### 4.3 Output File Management
- [ ] `MonthlySummary_OutputDirDoesNotExist_CreatesDirectory` - Auto-create dir
- [ ] `MonthlySummary_OutputFileExists_Overwrites` - Overwrites existing file
- [ ] `MonthlySummary_OutputPathInvalid_ShowsError` - Invalid output path
- [ ] `MonthlySummary_NoWritePermission_ShowsError` - Permission denied

---

### 5. Detail Export Tests

#### 5.1 CSV Detail Export
- [x] `DetailCsv_WithValidInput_CreatesValidOutput` - CSV detail export
- [ ] `DetailCsv_RemovesPayerAccountColumn` - Original account column removed
- [ ] `DetailCsv_AddsAnonymizedColumn_AccountIdAnon` - New column added
- [ ] `DetailCsv_PreservesAllOtherColumns` - No data loss
- [ ] `DetailCsv_PreservesRowOrder` - Same order as input

#### 5.2 Parquet Detail Export
- [x] `DetailParquet_WithValidInput_CreatesValidOutput` - Parquet detail export
- [ ] `DetailParquet_UsesSnappyCompression` - Compression applied
- [ ] `DetailParquet_RemovesPayerAccountColumn` - Original account removed
- [ ] `DetailParquet_AddsAnonymizedColumn` - account_id_anon added
- [ ] `DetailParquet_ReadableByDuckDB` - Can read back with DuckDB

---

### 6. AWS Athena Compatibility Tests

#### 6.1 Column Normalization
- [x] `Athena_NormalizedView_HasCompatibleColumns` - 9 key columns present
- [x] `Athena_Normalization_FollowsAWSRules` - Transformation rules correct
- [ ] `Athena_AllColumns_Normalized` - All 245 columns normalized

#### 6.2 Sample Query Execution
- [x] `Athena_MonthlyCostsByService_Executes` - AWS sample query #1
- [x] `Athena_TopResourcesByCost_Executes` - AWS sample query #2
- [x] `Athena_DailyUsageTrends_Executes` - AWS sample query #3
- [x] `Athena_AccountLevelAggregation_Executes` - AWS sample query #4
- [ ] `Athena_YearToDateCosts_Executes` - AWS sample query #5 (from spec)

---

### 7. Error Handling & Edge Cases

#### 7.1 File System Errors
- [ ] `Error_FileNotFound_ShowsHelpfulMessage` - Clear error message
- [ ] `Error_PermissionDenied_ShowsHelpfulMessage` - Clear error message
- [ ] `Error_DiskFull_ShowsHelpfulMessage` - Clear error message
- [ ] `Error_PathTooLong_HandlesGracefully` - Windows MAX_PATH

#### 7.2 Data Validation Errors
- [ ] `Error_EmptyInputFile_ShowsError` - No data to process
- [ ] `Error_InvalidCsvFormat_ShowsError` - Malformed CSV
- [ ] `Error_MissingRequiredColumns_ShowsError` - Missing bill/PayerAccountId
- [ ] `Error_TypeMismatch_HandlesGracefully` - String in numeric column

#### 7.3 Memory & Performance
- [ ] `Performance_LargeFile_CompletesWithinTimeLimit` - 1M+ rows in <60s
- [ ] `Performance_MultipleFiles_ProcessesInParallel` - Concurrent processing
- [ ] `Memory_LargeFile_StaysUnderLimit` - <1GB memory for 1M rows

---

### 8. Cross-Platform Tests

#### 8.1 Path Handling
- [ ] `Path_WindowsBackslash_NormalizesCorrectly` - Windows paths work
- [ ] `Path_UnixForwardSlash_ProcessesCorrectly` - Unix paths work
- [ ] `Path_MixedSeparators_HandlesGracefully` - Mixed separators
- [ ] `Path_SpacesInPath_HandlesCorrectly` - Paths with spaces
- [ ] `Path_UnicodeCharacters_HandlesCorrectly` - Non-ASCII paths

#### 8.2 Line Endings
- [ ] `LineEnding_CRLF_ProcessesCorrectly` - Windows line endings
- [ ] `LineEnding_LF_ProcessesCorrectly` - Unix line endings
- [ ] `LineEnding_Mixed_HandlesGracefully` - Mixed line endings

---

### 9. Integration Tests (End-to-End)

#### 9.1 Real-World Scenarios
- [x] `E2E_RealCurData_ProducesValidSummary` - Full pipeline test
- [ ] `E2E_MonthlyWorkflow_AllFormats` - CSV→Summary, Parquet→Detail
- [ ] `E2E_MultiMonth_MergesCorrectly` - Multiple months merged
- [ ] `E2E_LargeDataset_CompletesSuccessfully` - 1M+ rows end-to-end

#### 9.2 Regression Tests
- [ ] `Regression_ScientificNotation_HandlesCorrectly` - -5.72e-05 values
- [ ] `Regression_NegativeCosts_IncludesCredits` - AWS credits included
- [ ] `Regression_QuotedValues_ParsesCorrectly` - CSV quoted fields
- [ ] `Regression_FloatingPointPrecision_WithinTolerance` - Precision issues

---

## Test Metrics

### Current Coverage
- **Total Test Specs**: 103
- **Tests Implemented**: 17
- **Tests Remaining**: 86
- **Coverage**: 16.5%

### Priority Levels
- **P0 - Critical** (must have for v1.0): 25 tests
- **P1 - High** (should have for v1.0): 35 tests
- **P2 - Medium** (nice to have): 30 tests
- **P3 - Low** (future enhancement): 13 tests

### Testing Goals
1. **Phase 1 (MVP)**: Achieve 60% coverage (62 tests) - P0 + P1
2. **Phase 2 (Production)**: Achieve 80% coverage (82 tests) - P0 + P1 + P2
3. **Phase 3 (Comprehensive)**: Achieve 100% coverage (103 tests) - All

---

## Test Implementation Order

### Sprint 1: Critical Error Handling (P0)
1. Input validation tests (missing files, invalid paths)
2. Salt validation tests (missing, empty, env var)
3. Output validation tests (file creation, permissions)
4. Basic error message tests

### Sprint 2: Core Functionality (P0)
5. Parquet input tests
6. Schema detection tests
7. Edge case data tests (empty, single row, nulls)
8. Output format validation tests

### Sprint 3: Data Quality (P1)
9. Anonymization edge cases
10. Aggregation edge cases (nulls, negatives, zeros)
11. CSV/Parquet detail export validation
12. Cross-platform path tests

### Sprint 4: Performance & Integration (P1)
13. Large file performance tests
14. Multi-file processing tests
15. Memory limit tests
16. Full end-to-end workflows

### Sprint 5: Advanced Features (P2)
17. All columns normalization test
18. Advanced Athena queries
19. Regression tests
20. Unicode/special character tests
