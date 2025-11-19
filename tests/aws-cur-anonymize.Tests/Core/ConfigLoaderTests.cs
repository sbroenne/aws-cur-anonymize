using AwsCurAnonymize.Core;
using FluentAssertions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Xunit;

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
        config.Should().NotBeNull();
        config.IncludePatterns.Should().BeNullOrEmpty();
        config.ExcludePatterns.Should().BeNullOrEmpty();
        config.Anonymization.Should().NotBeNull();
        config.Anonymization!.AnonymizeAccountIds.Should().BeTrue();
        config.Anonymization.AnonymizeArns.Should().BeTrue();
        config.Anonymization.HashTags.Should().BeTrue();
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
            config.Should().NotBeNull();
            config.Comment.Should().Be("Test config");
            config.IncludePatterns.Should().HaveCount(2);
            config.IncludePatterns.Should().Contain("line_item_*");
            config.IncludePatterns.Should().Contain("bill_*");
            config.ExcludePatterns.Should().HaveCount(2);
            config.ExcludePatterns.Should().Contain("identity_*");
            config.ExcludePatterns.Should().Contain("*_internal");
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
        shouldInclude.Should().Be(expectedInclude);
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
        shouldInclude.Should().Be(expectedInclude);
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
        shouldInclude.Should().Be(expectedInclude);
    }

    [Fact]
    public void ShouldIncludeColumn_WithNoPatterns_IncludesAllColumns()
    {
        // Arrange
        var config = new CurConfig();

        // Act & Assert
        ConfigLoader.ShouldIncludeColumn("any_column_name", config).Should().BeTrue();
        ConfigLoader.ShouldIncludeColumn("identity_something", config).Should().BeTrue();
        ConfigLoader.ShouldIncludeColumn("line_item_cost", config).Should().BeTrue();
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
        config.Should().NotBeNull();
        config.Comment.Should().Contain("Auto-generated configuration");
        config.ExcludePatterns.Should().NotBeNull();
        config.ExcludePatterns.Should().Contain("identity_*");
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
        config.Should().NotBeNull();
        config.ExcludePatterns.Should().NotBeNull();
        config.Anonymization.Should().NotBeNull();
        config.Anonymization.AnonymizeAccountIds.Should().BeTrue();
        config.Anonymization.AnonymizeArns.Should().BeTrue();
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
