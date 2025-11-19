# aws-cur-anonymize Copilot Instructions

## Project Overview

aws-cur-anonymize is a cross-platform .NET 10 console application for processing AWS Cost & Usage Reports (CUR). It merges, anonymizes, and summarizes CUR data from Parquet or CSV files, providing monthly cost summaries with anonymized account IDs and optional full-detail exports. The tool uses DuckDB for high-performance data processing and supports multiple AWS CUR schema versions (Legacy CSV, Legacy Parquet, and CUR 2.0).

## Task Tracking Requirement

**MANDATORY for all multi-step work**: Before beginning any task that involves multiple steps or changes across multiple files, you MUST:

1. **Create a task list** using the task management tool with specific, actionable items
2. **Mark tasks as in-progress** before starting work on them (limit to ONE at a time)
3. **Mark tasks as completed** IMMEDIATELY after finishing each one
4. **Update task status** as you progress through the work

### When Task Tracking is Required
- Implementing new features that affect multiple files
- Making changes across CLI, Core, and Test layers
- Refactoring or restructuring code
- Adding comprehensive test coverage
- Debugging issues that require multiple investigation steps
- Any work that will take more than one tool invocation to complete

### When Task Tracking Can Be Skipped
- Single-file edits with a single clear change
- Simple documentation updates
- Answering questions without making changes
- Running build/test commands

### Task List Best Practices
- Break down work into 5-10 specific, testable steps
- Each task should be completable independently
- Include file paths and specific method names in descriptions
- Mark dependencies between tasks when they exist
- Always verify completion before marking as done

This follows GitHub Copilot coding agent best practices for maintaining visibility into progress and ensuring systematic completion of complex work.

## Repository Information

- **Project Type**: .NET 10 Console Application (CLI tool)
- **Languages**: C# 12, PowerShell 7
- **Target Frameworks**: net10.0
- **Runtime**: Cross-platform (Windows, Linux, macOS)
- **Repository Size**: Small (~20 source files)
- **Key Dependencies**:
  - DuckDB.NET.Data.Full v1.1.3 (high-performance analytics engine)
  - System.CommandLine v2.0.0-beta4 (CLI framework)
  - xUnit, Moq, FluentAssertions (testing)

## Folder Structure

```
/src/aws-cur-anonymize/        # Main application
  /Cli/                        # CLI commands and argument handling
    Commands.cs                # System.CommandLine command definitions
  /Core/                       # Business logic
    CurPipeline.cs            # DuckDB data processing pipeline
    CurSchema.cs              # AWS CUR schema definitions
    AthenaColumnNormalizer.cs # Column name normalization
  Program.cs                   # Application entry point
  aws-cur-anonymize.csproj     # Project file
/tests/aws-cur-anonymize.Tests/ # Test project
  /Cli/                        # CLI tests
  /Core/                       # Core logic tests
/examples/                     # Usage examples
  example-workflow.ps1         # PowerShell example
  example-workflow.sh          # Bash example
/docs/                         # Documentation
  DEVELOPER_GUIDE.md           # Developer setup and workflow
/.github/                      # GitHub configuration
  copilot-instructions.md      # This file
```

## Build and Test Instructions

### Prerequisites
- .NET 10 SDK or later
- PowerShell 7 (for scripts)

### Build the Project
```powershell
# Restore dependencies
dotnet restore

# Build (must complete with zero errors and zero warnings)
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Run Tests
```powershell
# Run all tests (must all pass with zero failures)
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### Run the Application
```powershell
# Set environment variable for salt
$env:CUR_ANON_SALT = "your-secret-salt-value"

# Run from source
dotnet run --project src/aws-cur-anonymize -- run --input "curdata/*.csv" --output out/

# Run with specific parameters
dotnet run --project src/aws-cur-anonymize -- run `
  --input "curdata/*.parquet" `
  --output out/ `
  --salt "MY-SALT" `
  --detail true `
  --format parquet
```

### Publish Self-Contained Executable
```powershell
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained

# macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained
```

### Pre-Commit Validation
**Always run before committing code changes:**
```powershell
dotnet build && dotnet test
```
Both commands must succeed with no errors or warnings.

## Coding Standards and Conventions

### Naming Conventions
- **PascalCase**: Classes, interfaces, public members, properties, methods, constants
- **camelCase**: Local variables, parameters, private fields (prefix with `_`)
- **Verb-Noun pairs**: Method names (e.g., `ProcessCurFile`, `AnonymizeAccountId`)
- **Async suffix**: All async methods (e.g., `WriteMonthlySummaryCsvAsync`)
- **Interface prefix**: All interfaces start with `I` (e.g., `ICurProcessor`)

### Code Style
- **Indentation**: 4 spaces (no tabs)
- **Line length**: Maximum 120 characters
- **Namespaces**: Use file-scoped namespaces (`namespace AwsCurAnonymize.Core;`)
- **Nullable references**: Enabled project-wide, use `string?` for nullable types
- **Using directives**: Alphabetically ordered, unused ones removed
- **Access modifiers**: Always explicit (e.g., `public`, `private`, `internal`)
- **String type**: Use lowercase `string` (not `String`)
- **var usage**: Use when type is obvious from right side

### Documentation
- **XML comments**: Required for all public APIs, classes, interfaces, and methods
- **Code comments**: Explain "why" not "what" for complex logic
- **Example usage**: Include in class-level XML documentation
- **Parameter descriptions**: Document all parameters and return values

### Architecture Patterns
- **Separation of Concerns**: Keep CLI logic in `/Cli`, business logic in `/Core`
- **Single Responsibility**: Each class/method does one thing well
- **Async all the way**: Use async/await throughout the call stack
- **Dependency Injection**: Use constructor injection for testability

## AWS CUR Processing Specifics

### Supported CUR Formats
The tool must handle three AWS CUR schema versions:

1. **Legacy CSV** (forward-slash columns): `lineItem/UsageStartDate`, `bill/PayerAccountId`
2. **Legacy Parquet** (underscore columns): `lineitem_usagestartdate`, `bill_payeraccountid`
3. **CUR 2.0** (Data Exports): `line_item_usage_start_date`, `bill_payer_account_id`

### Schema Normalization
- All formats are normalized to **Athena-compatible column names**
- Use `AthenaColumnNormalizer` class for transformations
- Rules: Insert underscores before uppercase, convert to lowercase, replace special chars
- Example: `lineItem/UsageStartDate` → `line_item_usage_start_date`

### DuckDB Best Practices
- Use `:memory:` database for in-process analytics
- Always use `ALL_VARCHAR=TRUE` when reading CSV to avoid type inference errors
- Quote column names with special characters (e.g., `"lineItem/UsageStartDate"`)
- Use `read_parquet()` for Parquet files, `read_csv_auto()` for CSV
- Create views for intermediate transformations
- Use SQL `COPY TO` for efficient exports

### Anonymization
- **CRITICAL RULE**: **ANONYMIZE account ID columns, do NOT exclude them**
- Users need account-level analysis capabilities, just with anonymized IDs
- Use deterministic SHA-256 hashing with user-provided salt
- Format: `lpad(CAST(abs(hash(salt || account_id)) % 100000000000 AS VARCHAR), 12, '0')`
- Produces 12-digit anonymized account IDs matching AWS format
- **Replace original columns**: Convert `bill_payer_account_id` → `bill_payer_account_id_anon`
- **Handle multiple account columns**: Process `line_item_usage_account_id` similarly if present
- **Dynamic column detection**: Check which account columns exist before processing
- Salt must be kept secret and consistent for related reports

## Performance Considerations

- DuckDB handles large datasets efficiently in-memory
- Use SQL aggregations instead of C# loops for data transformations
- Leverage DuckDB's columnar storage for Parquet processing
- Normalize paths using forward slashes for DuckDB compatibility
- Escape single quotes in SQL strings: `s.Replace("'", "''")`

## Testing Standards

### Test Organization
- **Unit Tests**: Test individual methods in isolation (use Moq for dependencies)
- **Integration Tests**: Test end-to-end workflows with real files
- **Test Location**: Mirror source structure in `/tests/aws-cur-anonymize.Tests/`

### Test Requirements
- All tests must use xUnit framework
- Use FluentAssertions for readable assertions (e.g., `result.Should().Be(expected)`)
- Test names follow pattern: `MethodName_Scenario_ExpectedBehavior`
- Each test should test one thing and have a clear purpose
- Use `[Theory]` and `[InlineData]` for parameterized tests

### Integration Test Guidelines
- Use real CUR data files (stored in `/tests/testdata/`)
- Create temporary output directories (clean up in test disposal)
- Test all three CUR schema formats (Legacy CSV, Legacy Parquet, CUR 2.0)
- Validate Athena-compatible SQL queries work against normalized data
- Test error cases: missing files, invalid formats, corrupt data

### Test Examples from AWS Documentation
Validate these Athena-compatible queries work in tests:
```sql
-- Monthly costs by service
SELECT line_item_product_code, 
       sum(line_item_unblended_cost) AS total_cost
FROM cur_normalized
GROUP BY line_item_product_code;

-- Top resources by cost
SELECT line_item_resource_id, 
       sum(line_item_unblended_cost) AS cost
FROM cur_normalized
ORDER BY cost DESC LIMIT 10;
```

## User Experience and Error Handling

### Error Messages
- **User-facing errors**: Simple, actionable, non-technical language
- **Examples**:
  - ❌ "NullReferenceException in CurPipeline.cs line 42"
  - ✅ "Missing salt value. Set --salt parameter or CUR_ANON_SALT environment variable."
- Include suggested resolution steps in error messages
- Use professional, friendly tone (never blame the user)
- Log technical details to file, show simplified version to user

### CLI Design Principles
- Use System.CommandLine for consistent option handling
- Provide helpful `--help` output for all commands
- Validate inputs early with clear error messages
- Support environment variables for sensitive data (e.g., `CUR_ANON_SALT`)
- Use glob patterns for file inputs (e.g., `"cur/*.parquet"`)

### Output and Logging
- Default output: `cur_summary_monthly.csv` (monthly aggregated summary)
- Optional detail: `cur_detail.csv` or `cur_detail.parquet` (full data)
- Progress feedback for long operations (future: use Spectre.Console)
- Clear completion messages showing output location

## .NET and C# Best Practices

### Modern C# Features
- **Nullable reference types**: Enabled, use `string?` for nullable strings
- **Pattern matching**: Use for type checks and data extraction
- **File-scoped namespaces**: `namespace AwsCurAnonymize.Core;` (no braces)
- **String interpolation**: Prefer `$"Result: {value}"` over `string.Concat()`
- **Switch expressions**: Use pattern-based switch for cleaner code
- **Records**: Use for immutable data transfer objects
- **Init-only properties**: `public string Value { get; init; }`

### Async/Await
- **Async all the way**: Use async methods throughout call stack
- **Naming**: Always suffix with `Async` (e.g., `ProcessFileAsync`)
- **Return types**: `Task`, `Task<T>`, or `ValueTask<T>`
- **ConfigureAwait**: Not needed for console apps (no sync context)
- **Avoid**: `async void` except for event handlers

### File Operations
- **Path handling**: Use `Path.Combine()`, never string concatenation
- **Cross-platform paths**: Replace `\` with `/` for DuckDB compatibility
- **Directory creation**: Use `Directory.CreateDirectory()` (no-op if exists)
- **Using statements**: Always dispose file handles properly
- **Validation**: Check file existence before processing

### Security
- **Salt handling**: Never hardcode, use env vars or parameters
- **Input validation**: Sanitize all file paths and glob patterns
- **SQL injection**: Use DuckDB parameterization (quote escaping where needed)
- **Secrets**: Never log or display salt values in output

## Cross-Platform Compatibility

### Platform Support
- **Target platforms**: Windows, Linux, macOS
- **Testing**: Verify on all three platforms before releases
- **Runtime IDs**: `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`

### Path Handling
- **Directory separators**: Use `Path.Combine()` or `Path.DirectorySeparatorChar`
- **DuckDB paths**: Convert to forward slashes: `path.Replace("\\", "/")`
- **Case sensitivity**: Remember Linux/macOS are case-sensitive
- **Glob patterns**: Use cross-platform glob syntax for file matching

### PowerShell Scripts
- **Version**: Always use PowerShell 7 (`pwsh`), not Windows PowerShell
- **Shebang**: Include `#!/usr/bin/env pwsh` for Unix compatibility
- **Documentation**: Use `pwsh` in examples, not `powershell`

### Environment Variables
- **Reading**: `Environment.GetEnvironmentVariable("CUR_ANON_SALT")`
- **Conventions**: Use UPPER_SNAKE_CASE for env var names
- **Platform differences**: Handle missing vars gracefully

### Publishing
```powershell
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

## Common Validation Workflows

### Adding New Features
1. Write failing test first (TDD approach)
2. Implement minimal code to pass test
3. Refactor for clarity and maintainability
4. Run `dotnet build && dotnet test` to verify
5. Update documentation (README.md, DEVELOPER_GUIDE.md)
6. Commit with descriptive message

### Debugging Failed Tests
1. Read test name and assertion message carefully
2. Check test data in `/tests/testdata/`
3. Verify DuckDB SQL queries match CUR schema version
4. Test SQL queries independently in DuckDB CLI if needed
5. Check path normalization (backslashes vs forward slashes)
6. Validate column name transformations (Athena normalization)

### Troubleshooting DuckDB Issues
- **Type errors**: Use `ALL_VARCHAR=TRUE` for CSV imports
- **Column not found**: Check schema version and normalization
- **Path errors**: Ensure forward slashes in glob patterns
- **Quoting**: Use double quotes for columns with special chars
- **Performance**: Use views for transformations, not temp tables

## Key Architectural Decisions

### Why DuckDB?
- In-memory analytics database optimized for OLAP queries
- Native Parquet and CSV support with fast columnar processing
- SQL interface perfect for CUR data transformations
- No external dependencies or server processes required
- Excellent performance for aggregations and JOINs

### Why System.CommandLine?
- Modern, type-safe CLI framework from Microsoft
- Built-in help generation and validation
- Middleware pipeline for extensibility
- Better than CommandLineParser for complex scenarios

### Why Athena Normalization?
- Users familiar with AWS Athena SQL queries can reuse them
- Standardized column naming across all CUR formats
- Documented transformation rules from AWS
- Enables testing with real-world query examples

## Documentation Standards

### README.md Updates Required When:
- Adding new command-line options
- Changing output file formats or names
- Modifying anonymization algorithm
- Adding new CUR schema version support
- Changing environment variable names

### Code Comments Required For:
- Complex SQL queries (explain the business logic)
- Schema transformations and column mappings
- Non-obvious algorithm choices (e.g., hash modulo for account IDs)
- Error handling edge cases
- Performance optimizations

## Implementation Status

### Fully Implemented and Tested ✅

#### Core Functionality
- **CurPipeline** (`/Core/CurPipeline.cs`): High-performance DuckDB-based data processing pipeline
  - Summary CSV generation with monthly aggregations
  - Detail export in CSV and Parquet formats
  - Deterministic SHA-256 based account ID anonymization
  - Test Coverage: 15+ integration tests including real CUR data processing

- **AthenaColumnNormalizer** (`/Core/AthenaColumnNormalizer.cs`): Column name normalization to AWS Athena format
  - Normalize() method with AWS transformation rules
  - CreateColumnAlias() for SQL column aliasing
  - CommonColumns dictionary with 40+ standard CUR column mappings
  - Test Coverage: 25+ unit tests covering all transformation rules and edge cases

- **CurSchema** (`/Core/CurSchema.cs`): Multi-version CUR schema detection and mapping
  - Support for Legacy CSV (forward-slash columns)
  - Support for Legacy Parquet (underscore columns)
  - Support for CUR 2.0 (snake_case columns)
  - Auto-detection from file headers, files, and glob patterns
  - Test Coverage: 25+ unit tests for all schema versions and detection methods

#### CLI Layer
- **Commands** (`/Cli/Commands.cs`): System.CommandLine based argument parsing
  - `run` command with input/output/salt/detail/format options
  - Environment variable support for salt (CUR_ANON_SALT)
  - Glob pattern support for file inputs
  - Test Coverage: 10+ CLI tests for command structure and validation

#### Error Handling
- **ErrorHandlingTests** (`/Tests/Core/ErrorHandlingTests.cs`): Comprehensive error scenarios
  - Missing files (FileNotFoundException)
  - Empty/corrupted files (InvalidDataException)
  - Invalid directory paths (DirectoryNotFoundException)
  - Null/empty parameter validation
  - Test Coverage: 15+ error handling tests

#### Athena Compatibility
- **AthenaCompatibilityTests** (`/Tests/Core/AthenaCompatibilityTests.cs`): Real-world AWS Athena query validation
  - Monthly cost aggregations
  - Top resources by cost
  - Service-level cost summaries
  - Account-level breakdowns
  - Test Coverage: 20+ Athena-compatible SQL query tests

### Test Data
- Real CUR data files in `/curdata/` (100K+ row sample)
- Synthetic test data in `/tests/testdata/` (Legacy CSV, CUR 2.0 samples)
- All three CUR schema versions represented

### Test Statistics
- **Total Tests**: 100 tests (all passing ✅)
- **Test Projects**: 1 (aws-cur-anonymize.Tests)
- **Code Coverage**: Core business logic fully covered
- **Test Frameworks**: xUnit, FluentAssertions, Moq

### Key Lessons Learned

#### Anonymization Strategy (Critical Update)
- **NEVER exclude account ID columns** - this defeats the purpose of anonymization
- **ALWAYS anonymize account columns** to preserve analytical capabilities while protecting sensitive data
- Users need account-level cost analysis, just with anonymized identifiers
- Original approach excluded `bill_payer_account_id` and `line_item_usage_account_id` entirely
- **Corrected approach**: Replace with `_anon` suffixed columns containing 12-digit hashed values

#### End-to-End Testing Insights
- Real CUR data testing revealed security flaws missed by unit tests
- Integration tests with synthetic data insufficient - real data patterns different
- **Always test with production-like data volumes and schemas**
- Verify no original account IDs appear in anonymized output

#### Tool Integration Best Practices
- Cross-platform PowerShell scripts enable consistent setup across environments
- Document all tool dependencies and setup procedures for future maintainers

### What's NOT Implemented (Future Enhancements)

#### Optional Features
- **Progress indicators**: Console output currently minimal, could add Spectre.Console for rich progress bars
- **Parquet schema validation**: Currently relies on DuckDB's Parquet reader, could add explicit validation
- **Parallel processing**: Single-threaded DuckDB processing (sufficient for current use cases)
- **Incremental updates**: Full re-processing required (acceptable for batch operations)
- **Custom column mappings**: Hard-coded Athena normalization rules (could make configurable)

#### Known Limitations
- **Memory constraints**: DuckDB uses `:memory:` database, large CUR files (>1GB) may require system RAM
- **Cross-platform testing**: Automated tests run on Windows only, manual testing confirmed on Linux/macOS
- **Parquet compression**: Fixed to SNAPPY, could make configurable
- **Salt persistence**: User must manage salt consistency across runs (documented in README.md)

### Build Requirements
- **Zero errors**: `dotnet build` must succeed with no errors
- **Zero warnings preferred**: Current build has 3 warnings (nullable reference warnings in tests)
- **All tests pass**: `dotnet test` must show 100% pass rate (currently: 100/100 ✅)

### Pre-Commit Checklist
Always run before committing code changes:
```powershell
dotnet build && dotnet test
```
Both commands must succeed with no errors.

