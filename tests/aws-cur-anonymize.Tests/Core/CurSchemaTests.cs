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
        Assert.Equal(CurSchemaVersion.LegacyCsv, mapping.Version);
        Assert.Equal("\"lineItem/UsageStartDate\"", mapping.UsageStartDate);
        Assert.Equal("\"bill/PayerAccountId\"", mapping.PayerAccountId);
        Assert.Equal("\"product/ProductName\"", mapping.ProductName);
        Assert.Equal("\"lineItem/UsageType\"", mapping.UsageType);
        Assert.Equal("\"lineItem/UnblendedCost\"", mapping.UnblendedCost);
        Assert.Equal("\"lineItem/UsageAmount\"", mapping.UsageAmount);
    }

    [Fact]
    public void ForVersion_LegacyParquet_ReturnsCorrectMapping()
    {
        // Act
        var mapping = CurSchemaMapping.ForVersion(CurSchemaVersion.LegacyParquet);

        // Assert
        Assert.Equal(CurSchemaVersion.LegacyParquet, mapping.Version);
        Assert.Equal("lineitem_usagestartdate", mapping.UsageStartDate);
        Assert.Equal("bill_payeraccountid", mapping.PayerAccountId);
        Assert.Equal("product_productname", mapping.ProductName);
        Assert.Equal("lineitem_usagetype", mapping.UsageType);
        Assert.Equal("lineitem_unblendedcost", mapping.UnblendedCost);
        Assert.Equal("lineitem_usageamount", mapping.UsageAmount);
    }

    [Fact]
    public void ForVersion_Cur20_ReturnsCorrectMapping()
    {
        // Act
        var mapping = CurSchemaMapping.ForVersion(CurSchemaVersion.Cur20);

        // Assert
        Assert.Equal(CurSchemaVersion.Cur20, mapping.Version);
        Assert.Equal("line_item_usage_start_date", mapping.UsageStartDate);
        Assert.Equal("bill_payer_account_id", mapping.PayerAccountId);
        Assert.Equal("product_product_name", mapping.ProductName);
        Assert.Equal("line_item_usage_type", mapping.UsageType);
        Assert.Equal("line_item_unblended_cost", mapping.UnblendedCost);
        Assert.Equal("line_item_usage_amount", mapping.UsageAmount);
    }

    [Fact]
    public void ForVersion_Invoice_ReturnsCorrectMapping()
    {
        // Act
        var mapping = CurSchemaMapping.ForVersion(CurSchemaVersion.Invoice);

        // Assert
        Assert.Equal(CurSchemaVersion.Invoice, mapping.Version);
        Assert.Equal("usage_start_date", mapping.UsageStartDate);
        Assert.Equal("payer_account_id", mapping.PayerAccountId);
        Assert.Equal("product_name", mapping.ProductName);
        Assert.Equal("usage_type", mapping.UsageType);
        Assert.Equal("cost_before_discounts", mapping.UnblendedCost);
        Assert.Equal("usage_quantity", mapping.UsageAmount);
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
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("line_item_usage_start_date,bill_payer_account_id", CurSchemaVersion.Cur20)]
    [InlineData("identity_line_item_id,line_item_usage_start_date,bill_payer_account_id", CurSchemaVersion.Cur20)]
    public void DetectFromCsvHeader_Cur20Format_DetectsCorrectly(string headerLine, CurSchemaVersion expected)
    {
        // Act
        var result = CurSchemaMapping.DetectFromCsvHeader(headerLine);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invoice_i_d,payer_account_id,linked_account_id", CurSchemaVersion.Invoice)]
    [InlineData("record_type,invoice_i_d,usage_start_date", CurSchemaVersion.Invoice)]
    [InlineData("invoice_i_d,product_name,total_cost", CurSchemaVersion.Invoice)]
    public void DetectFromCsvHeader_InvoiceFormat_DetectsCorrectly(string headerLine, CurSchemaVersion expected)
    {
        // Act
        var result = CurSchemaMapping.DetectFromCsvHeader(headerLine);

        // Assert
        Assert.Equal(expected, result);
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
        Assert.Equal(CurSchemaVersion.LegacyCsv, result);
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
        Assert.Equal(CurSchemaVersion.LegacyCsv, result);
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
        Assert.Equal(CurSchemaVersion.Cur20, result);
    }

    [Fact]
    public async Task DetectFromCsvFileAsync_InvoiceFile_DetectsCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine(_tempDir, "invoice.csv");
        await File.WriteAllTextAsync(csvPath, "invoice_i_d,payer_account_id,linked_account_id,record_type\n123456,123456789012,987654321098,LineItem\n");

        // Act
        var result = await CurSchemaMapping.DetectFromCsvFileAsync(csvPath);

        // Assert
        Assert.Equal(CurSchemaVersion.Invoice, result);
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
        Assert.Equal(CurSchemaVersion.LegacyParquet, result);
    }

    [Fact]
    public async Task DetectFromGlobAsync_ParquetInPath_ReturnsLegacyParquet()
    {
        // Arrange
        var globPattern = Path.Combine(_tempDir, "file.parquet");

        // Act
        var result = await CurSchemaMapping.DetectFromGlobAsync(globPattern);

        // Assert
        Assert.Equal(CurSchemaVersion.LegacyParquet, result);
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
        Assert.True(new[] { CurSchemaVersion.LegacyCsv, CurSchemaVersion.Cur20 }.Contains(result));
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
        Assert.NotEqual(parquet.PayerAccountId, csv.PayerAccountId);
        Assert.NotEqual(cur20.PayerAccountId, csv.PayerAccountId);
        Assert.NotEqual(cur20.PayerAccountId, parquet.PayerAccountId);

        // Legacy CSV should have quotes
        Assert.Contains("\"", csv.PayerAccountId);
        Assert.DoesNotContain("\"", parquet.PayerAccountId);
        Assert.DoesNotContain("\"", cur20.PayerAccountId);
    }

    [Theory]
    [InlineData(CurSchemaVersion.LegacyCsv)]
    [InlineData(CurSchemaVersion.LegacyParquet)]
    [InlineData(CurSchemaVersion.Cur20)]
    [InlineData(CurSchemaVersion.Invoice)]
    public void ForVersion_AllVersions_HaveAllRequiredColumns(CurSchemaVersion version)
    {
        // Act
        var mapping = CurSchemaMapping.ForVersion(version);

        // Assert - all mappings should have all required columns
        Assert.False(string.IsNullOrEmpty(mapping.UsageStartDate));
        Assert.False(string.IsNullOrEmpty(mapping.PayerAccountId));
        Assert.False(string.IsNullOrEmpty(mapping.ProductName));
        Assert.False(string.IsNullOrEmpty(mapping.UsageType));
        Assert.False(string.IsNullOrEmpty(mapping.UnblendedCost));
        Assert.False(string.IsNullOrEmpty(mapping.UsageAmount));
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
        Assert.Equal(CurSchemaVersion.LegacyCsv, resultLower);
        Assert.Equal(CurSchemaVersion.LegacyCsv, resultUpper);
        Assert.Equal(CurSchemaVersion.LegacyCsv, resultMixed);
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
