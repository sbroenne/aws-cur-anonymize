using FluentAssertions;
using Xunit;
using AwsCurAnonymize.Core;
using System.IO;
using System.Threading.Tasks;

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

        // Act
        Func<Task> act = async () => await CurSchemaMapping.DetectFromCsvFileAsync(nonExistentFile);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task DetectFromCsvFileAsync_ThrowsInvalidDataException_WhenFileIsEmpty()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, string.Empty);

            // Act
            Func<Task> act = async () => await CurSchemaMapping.DetectFromCsvFileAsync(tempFile);

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>();
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

            // Act
            Func<Task> act = async () => await CurSchemaMapping.DetectFromCsvFileAsync(tempFile);

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>();
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
        result.Should().Be(CurSchemaVersion.LegacyCsv);
    }

    [Fact]
    public void DetectFromCsvHeader_ReturnsLegacyCsv_WhenHeaderIsWhitespace()
    {
        // Act - defaults to LegacyCsv for whitespace
        var result = CurSchemaMapping.DetectFromCsvHeader("   \t  \n  ");

        // Assert
        result.Should().Be(CurSchemaVersion.LegacyCsv);
    }

    [Fact]
    public void DetectFromCsvHeader_ReturnsLegacyCsv_WhenHeaderHasUnknownColumns()
    {
        // Arrange - completely unrecognizable header defaults to LegacyCsv
        var invalidHeader = "random_col1,random_col2,unknown_field";

        // Act
        var result = CurSchemaMapping.DetectFromCsvHeader(invalidHeader);

        // Assert
        result.Should().Be(CurSchemaVersion.LegacyCsv);
    }

    [Fact]
    public async Task DetectFromGlobAsync_ThrowsDirectoryNotFoundException_WhenPathDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}", "*.csv");

        // Act
        Func<Task> act = async () => await CurSchemaMapping.DetectFromGlobAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<DirectoryNotFoundException>();
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

            // Act
            Func<Task> act = async () => await CurSchemaMapping.DetectFromGlobAsync(globPattern);

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
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
        result.Should().BeNull();
    }

    [Fact]
    public void Normalize_ReturnsOriginalValue_WhenColumnNameIsWhitespace()
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize("   ");

        // Assert
        result.Should().Be("   ");
    }

    [Fact]
    public void CreateColumnAlias_ReturnsValidAlias_WhenCalledWithValidColumn()
    {
        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias("lineItem/UsageStartDate");

        // Assert
        result.Should().Be("\"lineItem/UsageStartDate\" AS line_item_usage_start_date");
    }

    [Fact]
    public void CurSchemaMapping_ThrowsArgumentException_WhenVersionIsInvalid()
    {
        // Act
        Action act = () => CurSchemaMapping.ForVersion((CurSchemaVersion)999);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*version*");
    }
}
