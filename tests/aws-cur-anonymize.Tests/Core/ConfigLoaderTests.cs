using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AwsCurAnonymize.Tests.Core;

public class ConfigLoaderTests
{
    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    [Fact]
    public async Task LoadConfigAsync_WithNullPath_ReturnsDefaultConfig()
    {
        // Act
        var config = await ConfigLoader.LoadConfigAsync(null);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.IncludePatterns == null || config.IncludePatterns.Count == 0);
        Assert.True(config.ExcludePatterns == null || config.ExcludePatterns.Count == 0);
        Assert.NotNull(config.Anonymization);
        Assert.NotNull(config.Anonymization!.AnonymizationPatterns);
        Assert.Contains("payer_account_id", config.Anonymization.AnonymizationPatterns);
        Assert.Contains("linked_account_id", config.Anonymization.AnonymizationPatterns);
    }

    [Fact]
    public async Task LoadConfigAsync_WithValidYamlFile_LoadsConfiguration()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.yaml");
        try
        {
            var testConfig = new CurConfig
            {
                Comment = "Test config",
                IncludePatterns = new List<string> { "line_item_*", "bill_*" },
                ExcludePatterns = new List<string> { "identity_*", "*_internal" }
            };

            var yaml = YamlSerializer.Serialize(testConfig);
            await File.WriteAllTextAsync(tempFile, yaml);

            // Act
            var config = await ConfigLoader.LoadConfigAsync(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("Test config", config.Comment);
            Assert.Equal(2, config.IncludePatterns.Count);
            Assert.Contains("line_item_*", config.IncludePatterns);
            Assert.Contains("bill_*", config.IncludePatterns);
            Assert.Equal(2, config.ExcludePatterns.Count);
            Assert.Contains("identity_*", config.ExcludePatterns);
            Assert.Contains("*_internal", config.ExcludePatterns);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadConfigAsync_WithNonexistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await ConfigLoader.LoadConfigAsync("nonexistent.yaml")
        );
    }

    [Theory]
    [InlineData("identity_line_item_id", false)]
    [InlineData("data_internal", false)]
    [InlineData("line_item_cost", true)]
    [InlineData("bill_invoice_id", true)]
    public void ShouldIncludeColumn_WithExcludePattern_FiltersCorrectly(string columnName, bool expectedInclude)
    {
        // Arrange
        var config = new CurConfig
        {
            ExcludePatterns = new List<string> { "identity_*", "*_internal" }
        };

        // Act
        var shouldInclude = ConfigLoader.ShouldIncludeColumn(columnName, config);

        // Assert
        Assert.Equal(expectedInclude, shouldInclude);
    }

    [Theory]
    [InlineData("line_item_cost", true)]
    [InlineData("bill_invoice_id", true)]
    [InlineData("product_region", false)]
    [InlineData("pricing_public_on_demand_cost", false)]
    public void ShouldIncludeColumn_WithIncludePattern_FiltersCorrectly(string columnName, bool expectedInclude)
    {
        // Arrange
        var config = new CurConfig
        {
            IncludePatterns = new List<string> { "line_item_*", "bill_*" }
        };

        // Act
        var shouldInclude = ConfigLoader.ShouldIncludeColumn(columnName, config);

        // Assert
        Assert.Equal(expectedInclude, shouldInclude);
    }

    [Theory]
    [InlineData("line_item_cost", true)]
    [InlineData("identity_line_item_id", false)]
    [InlineData("bill_invoice_id", true)]
    [InlineData("data_internal", false)]
    public void ShouldIncludeColumn_WithBothPatterns_ExcludeTakesPrecedence(string columnName, bool expectedInclude)
    {
        // Arrange
        var config = new CurConfig
        {
            IncludePatterns = new List<string> { "line_item_*", "bill_*", "identity_*", "*_internal" },
            ExcludePatterns = new List<string> { "identity_*", "*_internal" }
        };

        // Act
        var shouldInclude = ConfigLoader.ShouldIncludeColumn(columnName, config);

        // Assert
        Assert.Equal(expectedInclude, shouldInclude);
    }

    [Fact]
    public void ShouldIncludeColumn_WithNoPatterns_IncludesAllColumns()
    {
        // Arrange
        var config = new CurConfig();

        // Act & Assert
        Assert.True(ConfigLoader.ShouldIncludeColumn("any_column_name", config));
        Assert.True(ConfigLoader.ShouldIncludeColumn("identity_something", config));
        Assert.True(ConfigLoader.ShouldIncludeColumn("line_item_cost", config));
    }

    [Fact]
    public void GenerateConfigFromColumns_SuggestsExcludePatterns()
    {
        // Arrange
        var columns = new List<string>
        {
            "bill_payer_account_id",
            "line_item_usage_account_id",
            "line_item_resource_id",
            "resource_tags",
            "line_item_unblended_cost",
            "identity_line_item_id",
            "identity_time_interval"
        };

        // Act
        var config = ConfigLoader.GenerateConfigFromColumns(columns);

        // Assert
        Assert.NotNull(config);
        Assert.Contains("Auto-generated configuration", config.Comment);
        Assert.NotNull(config.ExcludePatterns);
        Assert.Contains("identity_*", config.ExcludePatterns);
    }

    [Fact]
    public async Task LoadConfigAsync_WithCurConfigYaml_LoadsSuccessfully()
    {
        // Arrange - Use the actual cur-config.yaml from project root
        var projectRoot = FindProjectRoot();
        var configPath = Path.Combine(projectRoot, "cur-config.yaml");

        if (!File.Exists(configPath))
        {
            // Skip test if file doesn't exist
            Assert.True(true, "Skipping test - cur-config.yaml not found");
            return;
        }

        // Act
        var config = await ConfigLoader.LoadConfigAsync(configPath);

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.ExcludePatterns);
        Assert.NotNull(config.Anonymization);
        Assert.NotNull(config.Anonymization.AnonymizationPatterns);
        Assert.Contains("payer_account_id", config.Anonymization.AnonymizationPatterns);
        Assert.Contains("payer_account_name", config.Anonymization.AnonymizationPatterns);
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
}
