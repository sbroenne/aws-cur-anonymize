namespace AwsCurAnonymize.Tests.Core;

/// <summary>
/// Tests for error handling across all core components.
/// </summary>
public class ErrorHandlingTests
{
    [Fact]
    public async Task DetectFromCsvFileAsync_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.csv");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await CurSchemaMapping.DetectFromCsvFileAsync(nonExistentFile));
    }

    [Fact]
    public async Task DetectFromCsvFileAsync_ThrowsInvalidDataException_WhenFileIsEmpty()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, string.Empty);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(
                async () => await CurSchemaMapping.DetectFromCsvFileAsync(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DetectFromCsvFileAsync_ThrowsInvalidDataException_WhenHeaderIsMissing()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "\n\n\n");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(
                async () => await CurSchemaMapping.DetectFromCsvFileAsync(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void DetectFromCsvHeader_ReturnsLegacyCsv_WhenHeaderIsEmpty()
    {
        // Act - defaults to LegacyCsv for empty/unrecognized headers
        var result = CurSchemaMapping.DetectFromCsvHeader(string.Empty);

        // Assert
        Assert.Equal(CurSchemaVersion.LegacyCsv, result);
    }

    [Fact]
    public void DetectFromCsvHeader_ReturnsLegacyCsv_WhenHeaderIsWhitespace()
    {
        // Act - defaults to LegacyCsv for whitespace
        var result = CurSchemaMapping.DetectFromCsvHeader("   \t  \n  ");

        // Assert
        Assert.Equal(CurSchemaVersion.LegacyCsv, result);
    }

    [Fact]
    public void DetectFromCsvHeader_ReturnsLegacyCsv_WhenHeaderHasUnknownColumns()
    {
        // Arrange - completely unrecognizable header defaults to LegacyCsv
        var invalidHeader = "random_col1,random_col2,unknown_field";

        // Act
        var result = CurSchemaMapping.DetectFromCsvHeader(invalidHeader);

        // Assert
        Assert.Equal(CurSchemaVersion.LegacyCsv, result);
    }

    [Fact]
    public async Task DetectFromGlobAsync_ThrowsDirectoryNotFoundException_WhenPathDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}", "*.csv");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            async () => await CurSchemaMapping.DetectFromGlobAsync(nonExistentPath));
    }

    [Fact]
    public async Task DetectFromGlobAsync_ThrowsFileNotFoundException_WhenNoFilesMatch()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var globPattern = Path.Combine(tempDir, "*.csv");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await CurSchemaMapping.DetectFromGlobAsync(globPattern));
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void Normalize_ReturnsOriginalValue_WhenColumnNameIsNull()
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Normalize_ReturnsOriginalValue_WhenColumnNameIsWhitespace()
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize("   ");

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void CreateColumnAlias_ReturnsValidAlias_WhenCalledWithValidColumn()
    {
        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias("lineItem/UsageStartDate");

        // Assert
        Assert.Equal("\"lineItem/UsageStartDate\" AS line_item_usage_start_date", result);
    }

    [Fact]
    public void CurSchemaMapping_ThrowsArgumentException_WhenVersionIsInvalid()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CurSchemaMapping.ForVersion((CurSchemaVersion)999));
        Assert.Contains("version", ex.Message);
    }
}
