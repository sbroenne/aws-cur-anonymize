using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AwsCurAnonymize.Core;

/// <summary>
/// Loads and processes CUR anonymization configuration files in YAML format.
/// </summary>
public static class ConfigLoader
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    /// <summary>
    /// Loads configuration from a YAML file. Returns default config if path is null.
    /// </summary>
    /// <param name="configPath">Path to YAML config file, or null for defaults.</param>
    /// <returns>Configuration object.</returns>
    public static async Task<CurConfig> LoadConfigAsync(string? configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            return CurConfig.CreateDefault();
        }

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Config file not found: {configPath}");
        }

        var content = await File.ReadAllTextAsync(configPath);
        var config = YamlDeserializer.Deserialize<CurConfig>(content);

        if (config == null)
        {
            throw new InvalidDataException($"Invalid config file: {configPath}");
        }

        // Ensure nested objects are initialized
        config.Anonymization ??= new AnonymizationSettings();

        return config;
    }

    /// <summary>
    /// Saves configuration to a YAML file.
    /// </summary>
    public static async Task SaveConfigAsync(string configPath, CurConfig config)
    {
        var yaml = YamlSerializer.Serialize(config);
        await File.WriteAllTextAsync(configPath, yaml);
    }

    /// <summary>
    /// Checks if a column should be excluded based on include/exclude patterns.
    /// </summary>
    public static bool ShouldIncludeColumn(string columnName, CurConfig config)
    {
        // Apply include patterns first (whitelist)
        if (config.IncludePatterns != null && config.IncludePatterns.Count > 0)
        {
            if (!MatchesAnyPattern(columnName, config.IncludePatterns))
            {
                return false;
            }
        }

        // Then apply exclude patterns (blacklist)
        if (config.ExcludePatterns != null && config.ExcludePatterns.Count > 0)
        {
            if (MatchesAnyPattern(columnName, config.ExcludePatterns))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a column name matches any of the provided glob patterns.
    /// </summary>
    private static bool MatchesAnyPattern(string columnName, List<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (MatchesGlobPattern(columnName, pattern))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Matches a string against a glob pattern (* and ? wildcards).
    /// </summary>
    public static bool MatchesGlobPattern(string input, string pattern)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Generates a default configuration from a list of column names.
    /// Suggests appropriate patterns based on common CUR column patterns.
    /// </summary>
    public static CurConfig GenerateConfigFromColumns(List<string> columns)
    {
        var config = new CurConfig
        {
            Comment = "Auto-generated configuration. Edit patterns and anonymization settings as needed.",
            Anonymization = new AnonymizationSettings
            {
                AnonymizationPatterns = new List<string>
                {
                    "payer_account_id",
                    "linked_account_id",
                    "*_account_id"
                }
            },
            ExcludePatterns = new List<string>()
        };

        // Suggest excluding identity columns if they exist
        if (columns.Any(c => c.ToLowerInvariant().StartsWith("identity_")))
        {
            config.ExcludePatterns.Add("identity_*");
        }

        return config;
    }
}
