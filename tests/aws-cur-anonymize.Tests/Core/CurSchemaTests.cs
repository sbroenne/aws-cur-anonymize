using Xunit;
using FluentAssertions;
using AwsCurAnonymize.Core;

namespace AwsCurAnonymize.Tests.Core;

/// <summary>
/// Unit tests for CUR schema version detection and column mappings.
/// </summary>
public class CurSchemaTests : IDisposable
{
    private readonly string _tempDir;

    public CurSchemaTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cur-schema-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void ForVersion_LegacyCsv_ReturnsCorrectMapping()
    {
        // Act
        var mapping = CurSchemaMapping.ForVersion(CurSchemaVersion.LegacyCsv);

        // Assert
        mapping.Version.Should().Be(CurSchemaVersion.LegacyCsv);
        mapping.UsageStartDate.Should().Be("\"lineItem/UsageStartDate\"");
        mapping.PayerAccountId.Should().Be("\"bill/PayerAccountId\"");
        mapping.ProductName.Should().Be("\"product/ProductName\"");
        mapping.UsageType.Should().Be("\"lineItem/UsageType\"");
        mapping.UnblendedCost.Should().Be("\"lineItem/UnblendedCost\"");
        mapping.UsageAmount.Should().Be("\"lineItem/UsageAmount\"");
    }

    [Fact]
    public void ForVersion_LegacyParquet_ReturnsCorrectMapping()
    {
        // Act
        var mapping = CurSchemaMapping.ForVersion(CurSchemaVersion.LegacyParquet);

        // Assert
        mapping.Version.Should().Be(CurSchemaVersion.LegacyParquet);
        mapping.UsageStartDate.Should().Be("lineitem_usagestartdate");
        mapping.PayerAccountId.Should().Be("bill_payeraccountid");
        mapping.ProductName.Should().Be("product_productname");
        mapping.UsageType.Should().Be("lineitem_usagetype");
        mapping.UnblendedCost.Should().Be("lineitem_unblendedcost");
        mapping.UsageAmount.Should().Be("lineitem_usageamount");
    }

    [Fact]
    public void ForVersion_Cur20_ReturnsCorrectMapping()
    {
        // Act
        var mapping = CurSchemaMapping.ForVersion(CurSchemaVersion.Cur20);

        // Assert
        mapping.Version.Should().Be(CurSchemaVersion.Cur20);
        mapping.UsageStartDate.Should().Be("line_item_usage_start_date");
        mapping.PayerAccountId.Should().Be("bill_payer_account_id");
        mapping.ProductName.Should().Be("product_product_name");
        mapping.UsageType.Should().Be("line_item_usage_type");
        mapping.UnblendedCost.Should().Be("line_item_unblended_cost");
        mapping.UsageAmount.Should().Be("line_item_usage_amount");
    }

    [Theory]
    [InlineData("lineItem/UsageStartDate,bill/PayerAccountId", CurSchemaVersion.LegacyCsv)]
    [InlineData("identity/LineItemId,lineItem/UsageStartDate,bill/PayerAccountId", CurSchemaVersion.LegacyCsv)]
    [InlineData("bill/PayerAccountId,lineItem/ProductCode", CurSchemaVersion.LegacyCsv)]
    public void DetectFromCsvHeader_LegacyCsvFormat_DetectsCorrectly(string headerLine, CurSchemaVersion expected)
    {
        // Act
        var result = CurSchemaMapping.DetectFromCsvHeader(headerLine);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("line_item_usage_start_date,bill_payer_account_id", CurSchemaVersion.Cur20)]
    [InlineData("identity_line_item_id,line_item_usage_start_date,bill_payer_account_id", CurSchemaVersion.Cur20)]
    public void DetectFromCsvHeader_Cur20Format_DetectsCorrectly(string headerLine, CurSchemaVersion expected)
    {
        // Act
        var result = CurSchemaMapping.DetectFromCsvHeader(headerLine);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("lineitem_usagestartdate,bill_payeraccountid")]
    [InlineData("some_column,another_column")]
    [InlineData("")]
    public void DetectFromCsvHeader_UnknownFormat_DefaultsToLegacyCsv(string headerLine)
    {
        // Act
        var result = CurSchemaMapping.DetectFromCsvHeader(headerLine);

        // Assert
        result.Should().Be(CurSchemaVersion.LegacyCsv);
    }

    [Fact]
    public async Task DetectFromCsvFileAsync_LegacyCsvFile_DetectsCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "legacy.csv");
        await File.WriteAllTextAsync(csvPath, "lineItem/UsageStartDate,bill/PayerAccountId\n2025-01-01,123456789012\n");

        // Act
        var result = await CurSchemaMapping.DetectFromCsvFileAsync(csvPath);

        // Assert
        result.Should().Be(CurSchemaVersion.LegacyCsv);
    }

    [Fact]
    public async Task DetectFromCsvFileAsync_Cur20File_DetectsCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "cur20.csv");
        await File.WriteAllTextAsync(csvPath, "line_item_usage_start_date,bill_payer_account_id\n2025-01-01,123456789012\n");

        // Act
        var result = await CurSchemaMapping.DetectFromCsvFileAsync(csvPath);

        // Assert
        result.Should().Be(CurSchemaVersion.Cur20);
    }

    [Fact]
    public async Task DetectFromCsvFileAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "nonexistent.csv");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await CurSchemaMapping.DetectFromCsvFileAsync(csvPath));
    }

    [Fact]
    public async Task DetectFromCsvFileAsync_EmptyFile_ThrowsInvalidDataException()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "empty.csv");
        await File.WriteAllTextAsync(csvPath, "");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await CurSchemaMapping.DetectFromCsvFileAsync(csvPath));
    }

    [Fact]
    public async Task DetectFromGlobAsync_ParquetPattern_ReturnsLegacyParquet()
    {
        // Arrange
        var globPattern = Path.Combine(_tempDir, "*.parquet");

        // Act
        var result = await CurSchemaMapping.DetectFromGlobAsync(globPattern);

        // Assert
        result.Should().Be(CurSchemaVersion.LegacyParquet);
    }

    [Fact]
    public async Task DetectFromGlobAsync_ParquetInPath_ReturnsLegacyParquet()
    {
        // Arrange
        var globPattern = Path.Combine(_tempDir, "file.parquet");

        // Act
        var result = await CurSchemaMapping.DetectFromGlobAsync(globPattern);

        // Assert
        result.Should().Be(CurSchemaVersion.LegacyParquet);
    }

    [Fact]
    public async Task DetectFromGlobAsync_CsvPattern_SniffsFirstFile()
    {
        // Arrange
        var csvPath1 = Path.Combine(_tempDir, "file1.csv");
        var csvPath2 = Path.Combine(_tempDir, "file2.csv");
        await File.WriteAllTextAsync(csvPath1, "line_item_usage_start_date,bill_payer_account_id\n");
        await File.WriteAllTextAsync(csvPath2, "lineItem/UsageStartDate,bill/PayerAccountId\n");

        var globPattern = Path.Combine(_tempDir, "*.csv");

        // Act
        var result = await CurSchemaMapping.DetectFromGlobAsync(globPattern);

        // Assert - should detect based on first file alphabetically
        result.Should().BeOneOf(CurSchemaVersion.LegacyCsv, CurSchemaVersion.Cur20);
    }

    [Fact]
    public async Task DetectFromGlobAsync_NoMatchingFiles_ThrowsFileNotFoundException()
    {
        // Arrange
        var globPattern = Path.Combine(_tempDir, "*.csv");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await CurSchemaMapping.DetectFromGlobAsync(globPattern));
    }

    [Fact]
    public async Task DetectFromGlobAsync_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var globPattern = Path.Combine(_tempDir, "nonexistent-dir", "*.csv");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            async () => await CurSchemaMapping.DetectFromGlobAsync(globPattern));
    }

    [Fact]
    public void ForVersion_AllVersions_HaveDistinctMappings()
    {
        // Arrange & Act
        var csv = CurSchemaMapping.ForVersion(CurSchemaVersion.LegacyCsv);
        var parquet = CurSchemaMapping.ForVersion(CurSchemaVersion.LegacyParquet);
        var cur20 = CurSchemaMapping.ForVersion(CurSchemaVersion.Cur20);

        // Assert - all should have different column names
        csv.PayerAccountId.Should().NotBe(parquet.PayerAccountId);
        csv.PayerAccountId.Should().NotBe(cur20.PayerAccountId);
        parquet.PayerAccountId.Should().NotBe(cur20.PayerAccountId);

        // Legacy CSV should have quotes
        csv.PayerAccountId.Should().Contain("\"");
        parquet.PayerAccountId.Should().NotContain("\"");
        cur20.PayerAccountId.Should().NotContain("\"");
    }

    [Theory]
    [InlineData(CurSchemaVersion.LegacyCsv)]
    [InlineData(CurSchemaVersion.LegacyParquet)]
    [InlineData(CurSchemaVersion.Cur20)]
    public void ForVersion_AllVersions_HaveAllRequiredColumns(CurSchemaVersion version)
    {
        // Act
        var mapping = CurSchemaMapping.ForVersion(version);

        // Assert - all mappings should have all required columns
        mapping.UsageStartDate.Should().NotBeNullOrEmpty();
        mapping.PayerAccountId.Should().NotBeNullOrEmpty();
        mapping.ProductName.Should().NotBeNullOrEmpty();
        mapping.UsageType.Should().NotBeNullOrEmpty();
        mapping.UnblendedCost.Should().NotBeNullOrEmpty();
        mapping.UsageAmount.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DetectFromCsvHeader_CaseInsensitive_DetectsCorrectly()
    {
        // Arrange - test with different casings
        var headerLower = "lineitem/usagestartdate,bill/payeraccountid";
        var headerUpper = "LINEITEM/USAGESTARTDATE,BILL/PAYERACCOUNTID";
        var headerMixed = "LineItem/UsageStartDate,Bill/PayerAccountId";

        // Act
        var resultLower = CurSchemaMapping.DetectFromCsvHeader(headerLower);
        var resultUpper = CurSchemaMapping.DetectFromCsvHeader(headerUpper);
        var resultMixed = CurSchemaMapping.DetectFromCsvHeader(headerMixed);

        // Assert - all should detect as LegacyCsv
        resultLower.Should().Be(CurSchemaVersion.LegacyCsv);
        resultUpper.Should().Be(CurSchemaVersion.LegacyCsv);
        resultMixed.Should().Be(CurSchemaVersion.LegacyCsv);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
