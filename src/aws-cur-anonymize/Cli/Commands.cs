using System.ComponentModel;
using System.Reflection;
using AwsCurAnonymize.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AwsCurAnonymize.Cli;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "[input]")]
    [Description("Input file, directory, or glob pattern (e.g., file.csv, *.csv, data/, data/*.parquet). Default: *.csv and *.parquet in current directory")]
    public string? Input { get; init; }

    [CommandOption("--output")]
    [Description("Output directory. Default: out/")]
    public string? Output { get; init; }

    [CommandOption("--format")]
    [Description("Output format (csv or parquet)")]
    [DefaultValue("csv")]
    public string Format { get; init; } = "csv";

    [CommandOption("--salt")]
    [Description("Salt for anonymization (or use CUR_ANON_SALT env var)")]
    public string? Salt { get; init; }

    [CommandOption("--config")]
    [Description("Optional YAML config file for custom filtering")]
    public string? Config { get; init; }
}

public class RunCommand : AsyncCommand<RunSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        // Show banner
        ShowBanner();

        var salt = !string.IsNullOrWhiteSpace(settings.Salt)
            ? settings.Salt
            : Environment.GetEnvironmentVariable("CUR_ANON_SALT");

        if (string.IsNullOrWhiteSpace(salt))
        {
            // Auto-generate a random salt
            salt = Guid.NewGuid().ToString("N");
            AnsiConsole.MarkupLine("[yellow]⚠[/] No salt provided. Using auto-generated salt (anonymization will not be deterministic across runs).");
        }

        // Default input: *.csv and *.parquet in current directory
        var input = settings.Input;
        if (string.IsNullOrWhiteSpace(input))
        {
            var currentDir = Directory.GetCurrentDirectory();
            var csvFiles = Directory.GetFiles(currentDir, "*.csv");
            var parquetFiles = Directory.GetFiles(currentDir, "*.parquet");

            if (csvFiles.Length == 0 && parquetFiles.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]✗[/] No CSV or Parquet files found in current directory.");
                AnsiConsole.MarkupLine("[yellow]Tip:[/] Specify an input pattern, e.g., [cyan]aws-cur-anonymize \"data/*.csv\"[/]");
                return 1;
            }

            // Prefer CSV if both exist, otherwise use whatever is available
            input = csvFiles.Length > 0 ? "*.csv" : "*.parquet";
            AnsiConsole.MarkupLine($"[dim]Processing {(csvFiles.Length > 0 ? csvFiles.Length : parquetFiles.Length)} file(s) from current directory[/]");
        }
        else
        {
            // Expand input pattern to show what we're processing
            var inputPattern = ExpandInputPattern(input);
            AnsiConsole.MarkupLine($"[dim]Input: {inputPattern}[/]");
        }

        // Default output directory
        var output = settings.Output;
        if (string.IsNullOrWhiteSpace(output))
        {
            output = "out";
            AnsiConsole.MarkupLine("[dim]Using default output directory: out/[/]");
        }

        // Use provided config or look for default cur-config.yaml in working directory
        var config = settings.Config;
        if (string.IsNullOrWhiteSpace(config))
        {
            var defaultConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "cur-config.yaml");
            if (File.Exists(defaultConfigPath))
            {
                config = defaultConfigPath;
                AnsiConsole.MarkupLine($"[dim]Using config: {Path.GetFileName(config)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]✗[/] Configuration file not found.");
                AnsiConsole.MarkupLine("[yellow]Expected:[/] cur-config.yaml in current directory");
                AnsiConsole.MarkupLine("[yellow]Or specify:[/] --config path/to/config.yaml");
                return 1;
            }
        }
        else if (!File.Exists(config))
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Configuration file not found: [cyan]{config}[/]");
            return 1;
        }

        Directory.CreateDirectory(output);

        // Get list of files to process
        var filesToProcess = GetFilesToProcess(input);

        if (filesToProcess.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]✗[/] No files found matching input pattern.");
            return 1;
        }

        AnsiConsole.MarkupLine($"[dim]Found {filesToProcess.Count} file(s) to process[/]");

        // Process each file individually
        foreach (var file in filesToProcess)
        {
            var outputFile = Path.GetFileNameWithoutExtension(file);
            var schemaVersion = await DetectSchemaVersionAsync(file);

            AnsiConsole.MarkupLine($"\n[cyan]Processing:[/] {Path.GetFileName(file)}");
            ShowFileFormat(schemaVersion, file);

            CurProcessingStats stats = null!;
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Processing CUR data...", async ctx =>
                {
                    ctx.Status("Loading and normalizing CUR data...");
                    ctx.Status("Anonymizing account IDs and ARNs...");
                    stats = await CurPipeline.WriteDetailAsync(file, output, salt!, settings.Format, config, outputFile);
                });

            var detailFile = settings.Format == "parquet" ? $"{outputFile}.parquet" : $"{outputFile}.csv";
            AnsiConsole.MarkupLine($"[green]✓[/] Output: [cyan]{Path.Combine(output, detailFile)}[/]");

            // Display statistics
            AnsiConsole.MarkupLine($"[dim]  Columns: {stats.OriginalColumnCount} → {stats.OutputColumnCount}[/]");
            if (stats.AnonymizedAccountColumns > 0 || stats.AnonymizedArnColumns > 0 || stats.HashedTagColumns > 0)
            {
                var anonymized = new List<string>();
                if (stats.AnonymizedAccountColumns > 0) anonymized.Add($"{stats.AnonymizedAccountColumns} account");
                if (stats.AnonymizedArnColumns > 0) anonymized.Add($"{stats.AnonymizedArnColumns} ARN");
                if (stats.HashedTagColumns > 0) anonymized.Add($"{stats.HashedTagColumns} tag");
                AnsiConsole.MarkupLine($"[dim]  Anonymized: {string.Join(", ", anonymized)}[/]");
            }
        }

        AnsiConsole.MarkupLine($"\n[green]✓[/] All files processed successfully!");
        return 0;
    }

    private static void ShowBanner()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        AnsiConsole.Write(
            new FigletText("AWS CUR Anonymizer")
                .LeftJustified()
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine($"[dim]Version {version}[/]");
        AnsiConsole.WriteLine();
    }

    private static async Task<CurSchemaVersion> DetectSchemaVersionAsync(string inputPattern)
    {
        try
        {
            // If it's a specific file, detect from that file
            if (File.Exists(inputPattern))
            {
                return await CurSchemaMapping.DetectFromCsvFileAsync(inputPattern);
            }

            // If it's a glob pattern, find first matching file
            var directory = Path.GetDirectoryName(inputPattern) ?? Directory.GetCurrentDirectory();
            var pattern = Path.GetFileName(inputPattern);

            if (string.IsNullOrEmpty(pattern) || !pattern.Contains('*'))
            {
                pattern = "*.*";
            }

            var files = Directory.GetFiles(directory, pattern);
            if (files.Length > 0)
            {
                var firstFile = files[0];
                if (firstFile.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return await CurSchemaMapping.DetectFromCsvFileAsync(firstFile);
                }
                else if (firstFile.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase))
                {
                    return CurSchemaVersion.LegacyParquet; // Assume legacy parquet
                }
            }
        }
        catch
        {
            // Fallback to default
        }

        return CurSchemaVersion.Cur20; // Default fallback
    }

    private static void ShowFileFormat(CurSchemaVersion schemaVersion, string input)
    {
        var formatName = schemaVersion switch
        {
            CurSchemaVersion.LegacyCsv => "Legacy CSV (forward-slash columns)",
            CurSchemaVersion.LegacyParquet => "Legacy Parquet (underscore columns)",
            CurSchemaVersion.Cur20 => "CUR 2.0 (snake_case columns)",
            _ => "Unknown format"
        };

        var fileType = input.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase) ||
                       input.Contains("*.parquet", StringComparison.OrdinalIgnoreCase) ? "Parquet" : "CSV";
        AnsiConsole.MarkupLine($"[dim]Format detected: {formatName} ({fileType})[/]");
    }

    private static string ExpandInputPattern(string input)
    {
        // If it's a directory, show directory name
        if (Directory.Exists(input))
        {
            return $"{input} (directory)";
        }

        // If it contains wildcards, show the pattern
        if (input.Contains('*') || input.Contains('?'))
        {
            return input;
        }

        // If it's a file, show just the filename
        if (File.Exists(input))
        {
            return Path.GetFileName(input);
        }

        return input;
    }

    private static List<string> GetFilesToProcess(string input)
    {
        var files = new List<string>();

        // If it's a specific file
        if (File.Exists(input))
        {
            files.Add(input);
            return files;
        }

        // If it's a directory, get all CSV and Parquet files
        if (Directory.Exists(input))
        {
            files.AddRange(Directory.GetFiles(input, "*.csv"));
            files.AddRange(Directory.GetFiles(input, "*.parquet"));
            return files;
        }

        // If it's a glob pattern
        if (input.Contains('*') || input.Contains('?'))
        {
            var directory = Path.GetDirectoryName(input);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }

            var pattern = Path.GetFileName(input);
            files.AddRange(Directory.GetFiles(directory, pattern));
            return files;
        }

        return files;
    }

    private static string GetOutputFilename(string input)
    {
        // If it's a specific file, use that filename (without extension)
        if (File.Exists(input))
        {
            return Path.GetFileNameWithoutExtension(input);
        }

        // If it's a glob pattern, check how many files match
        if (input.Contains('*') || input.Contains('?'))
        {
            var directory = Path.GetDirectoryName(input);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }

            var pattern = Path.GetFileName(input);
            var files = Directory.GetFiles(directory, pattern);

            if (files.Length == 1)
            {
                // Single file - use its name
                return Path.GetFileNameWithoutExtension(files[0]);
            }
            else if (files.Length > 1)
            {
                // Multiple files - use generic name based on directory
                var dirName = Path.GetFileName(directory);
                if (string.IsNullOrEmpty(dirName))
                {
                    // Root or current directory
                    return "cur_merged";
                }
                return $"{dirName}_merged";
            }
        }

        // Fallback to "cur_detail" if we can't determine
        return "cur_detail";
    }
}
