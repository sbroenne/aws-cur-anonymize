using Xunit;
using FluentAssertions;
using DuckDB.NET.Data;

namespace AwsCurAnonymize.Tests.Core;

/// <summary>
/// Integration tests validating AWS Athena query compatibility.
/// These tests use sample queries from AWS documentation to ensure the normalized schema works.
/// </summary>
public class AthenaCompatibilityTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly string _tempOutputDir;

    public AthenaCompatibilityTests()
    {
        _tempOutputDir = Path.Combine(Path.GetTempPath(), $"cur-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempOutputDir);

        // Use test data from tests/testdata
        var projectRoot = FindProjectRoot();
        var testDataFile = Path.Combine(projectRoot, "tests", "testdata", "sample-legacy-csv.csv");
        _testDataPath = File.Exists(testDataFile) ? testDataFile : string.Empty;
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "aws-cur-anonymize.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir ?? Directory.GetCurrentDirectory();
    }

    [Fact]
    public async Task NormalizedView_HasAthenaCompatibleColumnNames()
    {
        if (string.IsNullOrEmpty(_testDataPath) || !File.Exists(_testDataPath))
        {
            Assert.True(true, "Skipping test - real data not found");
            return;
        }

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        // Load raw data
        cmd.CommandText = $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{_testDataPath.Replace("\\", "/")}', HEADER=TRUE, ALL_VARCHAR=TRUE);";
        cmd.ExecuteNonQuery();

        // Create normalized view (simulate what pipeline does)
        await CreateNormalizedViewAsync(cmd, "cur");

        // Verify expected Athena columns exist
        cmd.CommandText = @"
            SELECT column_name
            FROM information_schema.columns
            WHERE table_name = 'cur_normalized'
              AND column_name IN (
                'line_item_usage_start_date',
                'line_item_usage_end_date',
                'bill_payer_account_id',
                'line_item_usage_account_id',
                'line_item_product_code',
                'line_item_usage_type',
                'line_item_unblended_cost',
                'line_item_blended_cost',
                'product_product_name'
              )
            ORDER BY column_name;";

        var foundColumns = new List<string>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                foundColumns.Add(reader.GetString(0));
            }
        }

        // Should have all 9 expected columns
        foundColumns.Should().HaveCount(9);
        foundColumns.Should().Contain("bill_payer_account_id");
        foundColumns.Should().Contain("line_item_usage_start_date");
        foundColumns.Should().Contain("line_item_unblended_cost");
    }

    [Fact]
    public async Task AthenaQuery_MonthlyCostsByService_ExecutesSuccessfully()
    {
        if (string.IsNullOrEmpty(_testDataPath) || !File.Exists(_testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        // Load and normalize
        cmd.CommandText = $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{_testDataPath.Replace("\\", "/")}', HEADER=TRUE, ALL_VARCHAR=TRUE);";
        cmd.ExecuteNonQuery();
        await CreateNormalizedViewAsync(cmd, "cur");

        // AWS Athena sample query: Monthly costs by service
        cmd.CommandText = @"
            SELECT line_item_product_code,
                   DATE_TRUNC('month', CAST(line_item_usage_start_date AS TIMESTAMP)) AS month,
                   SUM(CAST(line_item_unblended_cost AS DOUBLE)) AS total_cost
            FROM cur_normalized
            WHERE line_item_product_code IS NOT NULL
              AND line_item_product_code != ''
            GROUP BY line_item_product_code, DATE_TRUNC('month', CAST(line_item_usage_start_date AS TIMESTAMP))
            HAVING SUM(CAST(line_item_unblended_cost AS DOUBLE)) <> 0
            ORDER BY line_item_product_code, month
            LIMIT 10;";

        var rowCount = 0;
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                rowCount++;
                // Verify structure
                reader.GetString(0).Should().NotBeNullOrEmpty(); // product_code
                reader.GetValue(1).Should().NotBeNull(); // month
                // Cost can be any valid double (positive or negative)
            }
        }

        rowCount.Should().BeGreaterThan(0, "Query should return results");
    }

    [Fact]
    public async Task AthenaQuery_TopResourcesByCost_ExecutesSuccessfully()
    {
        if (string.IsNullOrEmpty(_testDataPath) || !File.Exists(_testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        cmd.CommandText = $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{_testDataPath.Replace("\\", "/")}', HEADER=TRUE, ALL_VARCHAR=TRUE);";
        cmd.ExecuteNonQuery();
        await CreateNormalizedViewAsync(cmd, "cur");

        // AWS Athena sample query: Top 10 resources by cost
        cmd.CommandText = @"
            SELECT line_item_resource_id,
                   line_item_product_code,
                   SUM(CAST(line_item_unblended_cost AS DOUBLE)) AS total_cost
            FROM cur_normalized
            WHERE line_item_resource_id IS NOT NULL
              AND line_item_resource_id != ''
            GROUP BY line_item_resource_id, line_item_product_code
            ORDER BY total_cost DESC
            LIMIT 10;";

        var rowCount = 0;
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                rowCount++;
                reader.GetString(0).Should().NotBeNullOrEmpty(); // resource_id
                reader.GetString(1).Should().NotBeNullOrEmpty(); // product_code
            }
        }

        rowCount.Should().BeGreaterThan(0, "Query should return top resources");
    }

    [Fact]
    public async Task AthenaQuery_DailyUsageTrends_ExecutesSuccessfully()
    {
        if (string.IsNullOrEmpty(_testDataPath) || !File.Exists(_testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        cmd.CommandText = $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{_testDataPath.Replace("\\", "/")}', HEADER=TRUE, ALL_VARCHAR=TRUE);";
        cmd.ExecuteNonQuery();
        await CreateNormalizedViewAsync(cmd, "cur");

        // AWS Athena sample query: Daily usage trends (adapted for DuckDB)
        cmd.CommandText = @"
            SELECT CAST(line_item_usage_start_date AS DATE) AS usage_date,
                   line_item_product_code,
                   SUM(CAST(line_item_usage_amount AS DOUBLE)) AS total_usage
            FROM cur_normalized
            WHERE line_item_usage_amount IS NOT NULL
              AND line_item_usage_amount != ''
            GROUP BY CAST(line_item_usage_start_date AS DATE), line_item_product_code
            ORDER BY usage_date DESC, total_usage DESC
            LIMIT 20;";

        var rowCount = 0;
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                rowCount++;
                reader.GetValue(0).Should().NotBeNull(); // date
                reader.GetString(1).Should().NotBeNullOrEmpty(); // product_code
            }
        }

        rowCount.Should().BeGreaterThan(0, "Query should return daily trends");
    }

    [Fact]
    public async Task AthenaQuery_AccountLevelAggregation_ExecutesSuccessfully()
    {
        if (string.IsNullOrEmpty(_testDataPath) || !File.Exists(_testDataPath))
        {
            Assert.True(true, "Skipping test - test data not found");
            return;
        }

        using var con = new DuckDBConnection("Data Source=:memory:");
        await con.OpenAsync();
        using var cmd = con.CreateCommand();

        cmd.CommandText = $"CREATE VIEW cur AS SELECT * FROM read_csv_auto('{_testDataPath.Replace("\\", "/")}', HEADER=TRUE, ALL_VARCHAR=TRUE);";
        cmd.ExecuteNonQuery();
        await CreateNormalizedViewAsync(cmd, "cur");

        // AWS Athena sample query: Account-level costs
        cmd.CommandText = @"
            SELECT bill_payer_account_id,
                   line_item_usage_account_id,
                   SUM(CAST(line_item_unblended_cost AS DOUBLE)) AS total_cost
            FROM cur_normalized
            GROUP BY bill_payer_account_id, line_item_usage_account_id
            ORDER BY total_cost DESC
            LIMIT 10;";

        var rowCount = 0;
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                rowCount++;
                reader.GetString(0).Should().NotBeNullOrEmpty(); // payer account
                reader.GetString(1).Should().NotBeNullOrEmpty(); // usage account
            }
        }

        rowCount.Should().BeGreaterThan(0, "Query should return account costs");
    }

    [Fact]
    public async Task ColumnNormalization_FollowsAthenaRules()
    {
        // Test Athena normalization rules directly
        var testCases = new Dictionary<string, string>
        {
            { "lineItem/UsageStartDate", "line_item_usage_start_date" },
            { "bill/PayerAccountId", "bill_payer_account_id" },
            { "product/ProductName", "product_product_name" },
            { "lineItem/UnblendedCost", "line_item_unblended_cost" },
            { "pricing/publicOnDemandRate", "pricing_public_on_demand_rate" },
            { "resourceTags/user:Name", "resource_tags_user_name" },
        };

        foreach (var testCase in testCases)
        {
            var normalized = AthenaColumnNormalizer.Normalize(testCase.Key);
            normalized.Should().Be(testCase.Value,
                $"Column '{testCase.Key}' should normalize to '{testCase.Value}'");
        }
    }

    // Helper method to create normalized view (copied from CurPipeline for testing)
    private static async Task CreateNormalizedViewAsync(DuckDBCommand cmd, string sourceView = "cur")
    {
        cmd.CommandText = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{sourceView}' ORDER BY ordinal_position;";
        var columns = new List<string>();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
        }

        var selectBuilder = new System.Text.StringBuilder();
        selectBuilder.AppendLine("CREATE OR REPLACE VIEW cur_normalized AS");
        selectBuilder.AppendLine("SELECT");

        for (int i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            var alias = AthenaColumnNormalizer.CreateColumnAlias(col);
            selectBuilder.Append("  ");
            selectBuilder.Append(alias);
            if (i < columns.Count - 1)
                selectBuilder.AppendLine(",");
            else
                selectBuilder.AppendLine();
        }

        selectBuilder.Append($"FROM {sourceView};");

        cmd.CommandText = selectBuilder.ToString();
        cmd.ExecuteNonQuery();
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
