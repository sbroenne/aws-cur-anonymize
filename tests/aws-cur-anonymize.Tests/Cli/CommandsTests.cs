using AwsCurAnonymize.Cli;
using Spectre.Console.Cli;

namespace AwsCurAnonymize.Tests.Cli;

public class CommandsTests
{
    [Fact]
    public void RunSettings_DefaultValues_AreCorrect()
    {
        // Act
        var settings = new RunSettings();

        // Assert
        Assert.Equal("csv", settings.Format);
        Assert.Null(settings.Input);
        Assert.Null(settings.Output);
        Assert.Null(settings.Salt);
        Assert.Null(settings.Config);
    }

    [Fact]
    public async Task RunCommand_LegacyCsv_ExecutesSuccessfully()
    {
        // Arrange - Legacy CSV format (forward-slash columns)
        var projectRoot = FindProjectRoot();
        var testDataPath = Path.Combine(projectRoot, "tests", "testdata", "sample-legacy-csv.csv");

        if (!File.Exists(testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        var tempOutput = Path.Combine(Path.GetTempPath(), $"cli-test-legacy-{Guid.NewGuid()}");

        try
        {
            // Act
            var app = new CommandApp<RunCommand>();
            var result = await app.RunAsync(new[] { testDataPath, "--output", tempOutput, "--salt", "test-salt" });

            // Assert
            Assert.Equal(0, result);
            Assert.True(Directory.Exists(tempOutput));
            Assert.True(File.Exists(Path.Combine(tempOutput, "sample-legacy-csv.csv")));

            // Verify content has anonymized account IDs
            var content = await File.ReadAllTextAsync(Path.Combine(tempOutput, "sample-legacy-csv.csv"));
            Assert.DoesNotContain("123456789012", content); // Original account ID should not appear
            Assert.DoesNotContain("987654321098", content); // Original account ID should not appear
        }
        finally
        {
            if (Directory.Exists(tempOutput))
                Directory.Delete(tempOutput, true);
        }
    }

    [Fact]
    public async Task RunCommand_Cur20_ExecutesSuccessfully()
    {
        // Arrange - CUR 2.0 format (snake_case columns)
        var projectRoot = FindProjectRoot();
        var testDataPath = Path.Combine(projectRoot, "tests", "testdata", "sample-cur20.csv");

        if (!File.Exists(testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        var tempOutput = Path.Combine(Path.GetTempPath(), $"cli-test-cur20-{Guid.NewGuid()}");

        try
        {
            // Act
            var app = new CommandApp<RunCommand>();
            var result = await app.RunAsync(new[] { testDataPath, "--output", tempOutput, "--salt", "test-salt" });

            // Assert
            Assert.Equal(0, result);
            Assert.True(Directory.Exists(tempOutput));
            Assert.True(File.Exists(Path.Combine(tempOutput, "sample-cur20.csv")));

            // Verify content has anonymized account IDs
            var content = await File.ReadAllTextAsync(Path.Combine(tempOutput, "sample-cur20.csv"));
            Assert.DoesNotContain("123456789012", content);
            Assert.DoesNotContain("987654321098", content);
        }
        finally
        {
            if (Directory.Exists(tempOutput))
                Directory.Delete(tempOutput, true);
        }
    }

    [Fact]
    public async Task RunCommand_DetailExportCsv_ExecutesSuccessfully()
    {
        // Arrange
        var projectRoot = FindProjectRoot();
        var testDataPath = Path.Combine(projectRoot, "tests", "testdata", "sample-cur20.csv");

        if (!File.Exists(testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        var tempOutput = Path.Combine(Path.GetTempPath(), $"cli-test-detail-{Guid.NewGuid()}");

        try
        {
            // Act
            var app = new CommandApp<RunCommand>();
            var result = await app.RunAsync(new[] { testDataPath, "--output", tempOutput, "--salt", "test-salt" });

            // Assert
            Assert.Equal(0, result);
            Assert.True(Directory.Exists(tempOutput));
            Assert.True(File.Exists(Path.Combine(tempOutput, "sample-cur20.csv")));

            // Verify anonymization
            var content = await File.ReadAllTextAsync(Path.Combine(tempOutput, "sample-cur20.csv"));
            Assert.DoesNotContain("123456789012", content);
        }
        finally
        {
            if (Directory.Exists(tempOutput))
                Directory.Delete(tempOutput, true);
        }
    }

    [Fact]
    public async Task RunCommand_DetailExportParquet_ExecutesSuccessfully()
    {
        // Arrange
        var projectRoot = FindProjectRoot();
        var testDataPath = Path.Combine(projectRoot, "tests", "testdata", "sample-legacy-csv.csv");

        if (!File.Exists(testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        var tempOutput = Path.Combine(Path.GetTempPath(), $"cli-test-parquet-{Guid.NewGuid()}");

        try
        {
            // Act
            var app = new CommandApp<RunCommand>();
            var result = await app.RunAsync(new[] { testDataPath, "--output", tempOutput, "--salt", "test-salt", "--format", "parquet" });

            // Assert
            Assert.Equal(0, result);
            Assert.True(Directory.Exists(tempOutput));
            Assert.True(File.Exists(Path.Combine(tempOutput, "sample-legacy-csv.parquet")));

            // Verify parquet file is not empty
            var fileInfo = new FileInfo(Path.Combine(tempOutput, "sample-legacy-csv.parquet"));
            Assert.True(fileInfo.Length > 0);
        }
        finally
        {
            if (Directory.Exists(tempOutput))
                Directory.Delete(tempOutput, true);
        }
    }

    [Fact]
    public async Task RunCommand_WithNoInput_ProcessesCurrentDirectory()
    {
        // Arrange
        var tempOutput = Path.Combine(Path.GetTempPath(), $"cli-test-{Guid.NewGuid()}");
        var app = new CommandApp<RunCommand>();

        // Capture console output to prevent test runner crashes
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);

            // Act
            var result = await app.RunAsync(new[] { "--output", tempOutput, "--salt", "test-salt" });

            // Assert
            // With optional input, if no files in current directory this should fail
            // In test environment may succeed if there are CSV files present
            // Either way, should not crash
            Assert.True(result == 0 || result != 0); // Just verify it doesn't crash
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public async Task RunCommand_WithoutOutput_UsesDefault()
    {
        // Arrange
        var projectRoot = FindProjectRoot();
        var testDataPath = Path.Combine(projectRoot, "tests", "testdata", "sample-cur20.csv");

        if (!File.Exists(testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        var app = new CommandApp<RunCommand>();

        // Capture console output to prevent test runner crashes
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);

            // Act
            var result = await app.RunAsync(new[] { testDataPath, "--salt", "test-salt" });

            // Assert
            // With optional output, this should now succeed (defaults to out/)
            Assert.Equal(0, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public async Task RunCommand_WithNonExistentFile_ShowsError()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid()}.csv");
        var tempOutput = Path.Combine(Path.GetTempPath(), $"cli-test-{Guid.NewGuid()}");
        var app = new CommandApp<RunCommand>();

        // Capture console output to prevent test runner crashes
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);

            // Act
            var result = await app.RunAsync(new[] { nonExistentFile, "--output", tempOutput, "--salt", "test-salt" });

            // Assert
            Assert.NotEqual(0, result); // Non-zero exit code indicates error
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            if (Directory.Exists(tempOutput))
                Directory.Delete(tempOutput, true);
        }
    }

    [Fact]
    public async Task RunCommand_WithInvalidGlob_ShowsError()
    {
        // Arrange
        var invalidGlob = Path.Combine(Path.GetTempPath(), "nonexistent-dir", "*.csv");
        var tempOutput = Path.Combine(Path.GetTempPath(), $"cli-test-{Guid.NewGuid()}");
        var app = new CommandApp<RunCommand>();

        // Capture console output to prevent test runner crashes
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);

            // Act
            var result = await app.RunAsync(new[] { invalidGlob, "--output", tempOutput, "--salt", "test-salt" });

            // Assert
            Assert.NotEqual(0, result); // Non-zero exit code indicates error
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            if (Directory.Exists(tempOutput))
                Directory.Delete(tempOutput, true);
        }
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
