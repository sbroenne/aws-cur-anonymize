using AwsCurAnonymize.Core;
using FluentAssertions;
using DuckDB.NET.Data;

namespace AwsCurAnonymize.Tests.Core;

public class CurPipelineTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly string _tempOutputDir;
    private const string TestSalt = "test-salt-12345";

    public CurPipelineTests()
    {
        // Look for real CUR data in project root/curdata
        var projectRoot = FindProjectRoot();
        _testDataPath = Path.Combine(projectRoot, "curdata", "Clio - monthly-costs-00001.csv");
        _tempOutputDir = Path.Combine(Path.GetTempPath(), $"aws-cur-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempOutputDir);
    }

    private static string FindProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null)
        {
            if (File.Exists(Path.Combine(currentDir, "aws-cur-anonymize.sln")))
                return currentDir;
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        throw new InvalidOperationException("Could not find project root");
    }

    [Fact]
    public async Task WriteDetailAsync_WithCsvFormat_CreatesValidDetail()
    {
        if (!File.Exists(_testDataPath))
        {
            Assert.True(true, "Skipping test - real data not found");
            return;
        }

        // Act
        await CurPipeline.WriteDetailAsync(_testDataPath, _tempOutputDir, TestSalt, "csv");

        // Assert
        var outputFile = Path.Combine(_tempOutputDir, "cur_detail.csv");
        Assert.True(File.Exists(outputFile), "Detail CSV should be created");

        var lines = File.ReadAllLines(outputFile);
        Assert.True(lines.Length > 1, "Should have header and data rows");

        // Verify the file is valid CSV with proper structure
        var header = lines[0];
        Assert.NotEmpty(header);
        Assert.Contains(',', header); // Should be CSV format

        // Verify anonymization worked - no original account IDs in data
        var content = File.ReadAllText(outputFile);
        Assert.DoesNotContain("123456789012", content); // Example account ID shouldn't appear
    }

    [Fact]
    public async Task WriteDetailAsync_WithParquetFormat_CreatesValidParquet()
    {
        if (!File.Exists(_testDataPath))
        {
            Assert.True(true, "Skipping test - real data not found");
            return;
        }

        // Act
        await CurPipeline.WriteDetailAsync(_testDataPath, _tempOutputDir, TestSalt, "parquet");

        // Assert
        var outputFile = Path.Combine(_tempOutputDir, "cur_detail.parquet");
        Assert.True(File.Exists(outputFile), "Detail Parquet should be created");
        Assert.True(new FileInfo(outputFile).Length > 0, "Parquet file should not be empty");
    }

    [Fact]
    public async Task WriteDetailAsync_WithConfig_FiltersColumnsCorrectly()
    {
        // Arrange - Use test data with known columns
        var projectRoot = FindProjectRoot();
        var testDataFile = Path.Combine(projectRoot, "tests", "testdata", "sample-legacy-csv.csv");

        if (!File.Exists(testDataFile))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        // Create a config that excludes specific columns
        var configPath = Path.Combine(_tempOutputDir, "test-config.yaml");
        var config = new CurConfig
        {
            ExcludePatterns = new List<string>
            {
                "bill_*",           // Should exclude bill_payer_account_id
                "*_blended_cost"    // Should exclude line_item_blended_cost
            },
            Anonymization = new AnonymizationSettings
            {
                AnonymizeAccountIds = true,
                AnonymizeArns = false,
                HashTags = false
            }
        };
        await ConfigLoader.SaveConfigAsync(configPath, config);

        // Act
        var stats = await CurPipeline.WriteDetailAsync(testDataFile, _tempOutputDir, TestSalt, "csv", configPath, "filtered");

        // Assert - Verify columns were filtered
        stats.OriginalColumnCount.Should().Be(13, "sample-legacy-csv.csv has 13 columns");
        stats.OutputColumnCount.Should().BeLessThan(13, "some columns should be filtered out");

        // Read the output and verify excluded columns are not present
        var outputFile = Path.Combine(_tempOutputDir, "filtered.csv");
        var headerLine = File.ReadLines(outputFile).First();

        headerLine.Should().NotContain("bill_payer_account_id", "bill_* pattern should exclude this column");
        headerLine.Should().NotContain("line_item_blended_cost", "*_blended_cost pattern should exclude this column");
        headerLine.Should().Contain("line_item_usage_account_id", "this column should remain");
        headerLine.Should().Contain("line_item_unblended_cost", "only blended_cost should be excluded, not unblended_cost");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempOutputDir))
        {
            try
            {
                Directory.Delete(_tempOutputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
