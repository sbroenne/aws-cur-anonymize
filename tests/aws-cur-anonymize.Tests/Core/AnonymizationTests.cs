namespace AwsCurAnonymize.Tests.Core;

/// <summary>
/// Tests to verify that ALL account IDs are properly anonymized (replaced with hashed values, not excluded).
/// </summary>
public class AnonymizationTests
{
    [Fact]
    public async Task WriteDetailAsync_ExcludesAllAccountIds_NoOriginalAccountIdsInOutput()
    {
        // Arrange
        var tempInput = Path.GetTempFileName();
        var tempOutput = Path.Combine(Path.GetTempPath(), $"anon-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempOutput);

        try
        {
            // Create sample CUR data with both payer and usage account IDs
            var csvContent = @"bill/PayerAccountId,lineItem/UsageAccountId,lineItem/ProductCode,lineItem/UnblendedCost,lineItem/UsageAmount,product/ProductName,lineItem/UsageStartDate
123456789012,987654321098,AmazonEC2,10.50,100.0,Amazon Elastic Compute Cloud,2024-01-01T00:00:00Z
999888777666,999888777666,AmazonRDS,15.75,150.0,Amazon Relational Database Service,2024-01-01T00:00:00Z";

            File.WriteAllText(tempInput, csvContent);

            // Act
            await CurPipeline.WriteDetailAsync(tempInput, tempOutput, "TEST-SALT", "csv");

            // Assert
            var outputFile = Path.Combine(tempOutput, "cur_detail.csv");
            Assert.True(File.Exists(outputFile));

            var outputContent = File.ReadAllText(outputFile);

            // CRITICAL: Original account IDs must NOT appear in output
            Assert.DoesNotContain("123456789012", outputContent);
            Assert.DoesNotContain("987654321098", outputContent);
            Assert.DoesNotContain("999888777666", outputContent);

            // Account ID columns should exist with anonymized values
            var headerLine = File.ReadLines(outputFile).First();
            Assert.Contains("bill_payer_account_id", headerLine);
            Assert.Contains("line_item_usage_account_id", headerLine);

            // All account IDs should be 12-digit numbers
            var dataLines = File.ReadAllLines(outputFile).Skip(1);
            foreach (var line in dataLines)
            {
                var fields = line.Split(',');
                // Verify account ID fields are 12-digit numbers (anonymized format)
                Assert.True(fields.Any(f => f.Length == 12 && f.All(char.IsDigit)));
            }
        }
        finally
        {
            File.Delete(tempInput);
            if (Directory.Exists(tempOutput))
            {
                Directory.Delete(tempOutput, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WriteDetailAsync_AnonymizesMultipleAccounts_DeterministicHashing()
    {
        // Arrange
        var tempInput = Path.GetTempFileName();
        var tempOutput = Path.Combine(Path.GetTempPath(), $"anon-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempOutput);

        try
        {
            var csvContent = @"bill/PayerAccountId,lineItem/UsageAccountId,lineItem/ProductCode,lineItem/UnblendedCost,product/ProductName,lineItem/UsageStartDate
123456789012,123456789012,AmazonEC2,10.00,Amazon Elastic Compute Cloud,2024-01-01T00:00:00Z
123456789012,987654321098,AmazonRDS,20.00,Amazon Relational Database Service,2024-01-01T00:00:00Z";

            File.WriteAllText(tempInput, csvContent);

            // Act - run twice with same salt
            await CurPipeline.WriteDetailAsync(tempInput, tempOutput, "CONSISTENT-SALT", "csv");

            var firstRun = File.ReadAllText(Path.Combine(tempOutput, "cur_detail.csv"));
            File.Delete(Path.Combine(tempOutput, "cur_detail.csv"));

            await CurPipeline.WriteDetailAsync(tempInput, tempOutput, "CONSISTENT-SALT", "csv");
            var secondRun = File.ReadAllText(Path.Combine(tempOutput, "cur_detail.csv"));

            // Assert - outputs should be identical (deterministic)
            Assert.Equal(firstRun, secondRun);

            // Change salt and verify different output
            File.Delete(Path.Combine(tempOutput, "cur_detail.csv"));
            await CurPipeline.WriteDetailAsync(tempInput, tempOutput, "DIFFERENT-SALT", "csv");
            var differentSalt = File.ReadAllText(Path.Combine(tempOutput, "cur_detail.csv"));

            Assert.NotEqual(firstRun, differentSalt);
        }
        finally
        {
            File.Delete(tempInput);
            if (Directory.Exists(tempOutput))
            {
                Directory.Delete(tempOutput, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WriteDetailAsync_AnonymizedIdFormat_Is12DigitNumeric()
    {
        // Arrange
        var tempInput = Path.GetTempFileName();
        var tempOutput = Path.Combine(Path.GetTempPath(), $"anon-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempOutput);

        try
        {
            var csvContent = @"bill/PayerAccountId,lineItem/ProductCode,lineItem/UnblendedCost,product/ProductName,lineItem/UsageStartDate
123456789012,AmazonEC2,10.00,Amazon Elastic Compute Cloud,2024-01-01T00:00:00Z";

            File.WriteAllText(tempInput, csvContent);

            // Act
            await CurPipeline.WriteDetailAsync(tempInput, tempOutput, "FORMAT-TEST", "csv");

            // Assert
            var outputFile = Path.Combine(tempOutput, "cur_detail.csv");
            var headerLine = File.ReadLines(outputFile).First();
            var dataLine = File.ReadLines(outputFile).Skip(1).First();

            // Verify header contains account ID columns
            Assert.Contains("bill_payer_account_id", headerLine);

            // Extract account ID from data line
            var headers = headerLine.Split(',');
            var values = dataLine.Split(',');
            var payerAccountIndex = Array.IndexOf(headers, "bill_payer_account_id");
            var anonymizedId = values[payerAccountIndex];

            Assert.Matches(@"^\d{12}$", anonymizedId);
        }
        finally
        {
            File.Delete(tempInput);
            if (Directory.Exists(tempOutput))
            {
                Directory.Delete(tempOutput, recursive: true);
            }
        }
    }
}
