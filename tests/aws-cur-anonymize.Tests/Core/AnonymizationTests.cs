using FluentAssertions;
using Xunit;
using AwsCurAnonymize.Core;
using System.IO;
using System.Threading.Tasks;

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
            File.Exists(outputFile).Should().BeTrue();

            var outputContent = File.ReadAllText(outputFile);

            // CRITICAL: Original account IDs must NOT appear in output
            outputContent.Should().NotContain("123456789012", "original account IDs should be anonymized");
            outputContent.Should().NotContain("987654321098", "original account IDs should be anonymized");
            outputContent.Should().NotContain("999888777666", "original account IDs should be anonymized");

            // Account ID columns should exist with anonymized values
            var headerLine = File.ReadLines(outputFile).First();
            headerLine.Should().Contain("bill_payer_account_id", "payer account column should be present with anonymized values");
            headerLine.Should().Contain("line_item_usage_account_id", "usage account column should be present with anonymized values");

            // All account IDs should be 12-digit numbers
            var dataLines = File.ReadAllLines(outputFile).Skip(1);
            foreach (var line in dataLines)
            {
                var fields = line.Split(',');
                // Verify account ID fields are 12-digit numbers (anonymized format)
                fields.Should().Contain(f => f.Length == 12 && f.All(char.IsDigit), "all account IDs should be anonymized to 12-digit format");
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
            firstRun.Should().Be(secondRun, "same salt should produce same anonymized IDs");

            // Change salt and verify different output
            File.Delete(Path.Combine(tempOutput, "cur_detail.csv"));
            await CurPipeline.WriteDetailAsync(tempInput, tempOutput, "DIFFERENT-SALT", "csv");
            var differentSalt = File.ReadAllText(Path.Combine(tempOutput, "cur_detail.csv"));

            differentSalt.Should().NotBe(firstRun, "different salt should produce different anonymized IDs");
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
            headerLine.Should().Contain("bill_payer_account_id");

            // Extract account ID from data line
            var headers = headerLine.Split(',');
            var values = dataLine.Split(',');
            var payerAccountIndex = Array.IndexOf(headers, "bill_payer_account_id");
            var anonymizedId = values[payerAccountIndex];

            anonymizedId.Should().MatchRegex(@"^\d{12}$", "anonymized ID should be 12-digit numeric string");
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
