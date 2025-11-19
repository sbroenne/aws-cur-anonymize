using Xunit;
using FluentAssertions;
using AwsCurAnonymize.Core;

namespace AwsCurAnonymize.Tests.Core;

/// <summary>
/// Unit tests for AthenaColumnNormalizer to verify AWS Athena naming convention transformation rules.
/// </summary>
public class AthenaColumnNormalizerTests
{
    [Theory]
    [InlineData("lineItem/UsageStartDate", "line_item_usage_start_date")]
    [InlineData("bill/PayerAccountId", "bill_payer_account_id")]
    [InlineData("product/ProductName", "product_product_name")]
    [InlineData("lineItem/UnblendedCost", "line_item_unblended_cost")]
    [InlineData("pricing/publicOnDemandRate", "pricing_public_on_demand_rate")]
    [InlineData("lineItem/LineItemType", "line_item_line_item_type")]
    public void Normalize_LegacyCsvColumns_TransformsToAthenaFormat(string input, string expected)
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("lineitem_usagestartdate", "lineitem_usagestartdate")]
    [InlineData("bill_payeraccountid", "bill_payeraccountid")]
    [InlineData("product_productname", "product_productname")]
    public void Normalize_LegacyParquetColumns_AlreadyNormalized(string input, string expected)
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("line_item_usage_start_date", "line_item_usage_start_date")]
    [InlineData("bill_payer_account_id", "bill_payer_account_id")]
    [InlineData("product_product_name", "product_product_name")]
    public void Normalize_Cur20Columns_AlreadyNormalized(string input, string expected)
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("resourceTags/user:Name", "resource_tags_user_name")]
    [InlineData("resourceTags/aws:cloudformation:stack-name", "resource_tags_aws_cloudformation_stack_name")]
    [InlineData("costCategory/Environment", "cost_category_environment")]
    public void Normalize_TagAndCategoryColumns_HandlesSpecialCharacters(string input, string expected)
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("product/maxIopsvolume", "product_max_iopsvolume")]
    [InlineData("product/vcpu", "product_vcpu")]
    [InlineData("product/instanceType", "product_instance_type")]
    public void Normalize_MixedCaseColumns_InsertsUnderscoresCorrectly(string input, string expected)
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_EmptyOrWhitespace_ReturnsInput(string input)
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void Normalize_Null_ReturnsNull()
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Normalize_LeadingUnderscore_RemovesIt()
    {
        // Arrange
        var input = "_column/Name";

        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().NotStartWith("_");
        result.Should().Be("column_name");
    }

    [Fact]
    public void Normalize_TrailingUnderscore_RemovesIt()
    {
        // Arrange
        var input = "column/Name_";

        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().NotEndWith("_");
        result.Should().Be("column_name");
    }

    [Fact]
    public void Normalize_ConsecutiveSpecialChars_ConsolidatesToSingleUnderscore()
    {
        // Arrange
        var input = "column//Name";

        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().NotContain("__");
        result.Should().Be("column_name");
    }

    [Fact]
    public void Normalize_NumbersInColumnName_PreservesNumbers()
    {
        // Arrange
        var input = "product/max32IopsVolume";

        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().Contain("32");
        result.Should().Be("product_max32_iops_volume");
    }

    [Fact]
    public void CreateColumnAlias_WithForwardSlash_QuotesOriginalName()
    {
        // Arrange
        var input = "lineItem/UsageStartDate";

        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias(input);

        // Assert
        result.Should().StartWith("\"lineItem/UsageStartDate\"");
        result.Should().Contain(" AS ");
        result.Should().EndWith("line_item_usage_start_date");
    }

    [Fact]
    public void CreateColumnAlias_WithSpaces_QuotesOriginalName()
    {
        // Arrange
        var input = "column name";

        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias(input);

        // Assert
        result.Should().StartWith("\"column name\"");
        result.Should().Contain(" AS ");
        result.Should().EndWith("column_name");
    }

    [Fact]
    public void CreateColumnAlias_WithHyphen_QuotesOriginalName()
    {
        // Arrange
        var input = "column-name";

        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias(input);

        // Assert
        result.Should().StartWith("\"column-name\"");
        result.Should().Contain(" AS ");
        result.Should().EndWith("column_name");
    }

    [Fact]
    public void CreateColumnAlias_SimpleColumnName_DoesNotQuote()
    {
        // Arrange
        var input = "simplecolumn";

        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias(input);

        // Assert
        result.Should().NotContain("\"");
        result.Should().Be("simplecolumn AS simplecolumn");
    }

    [Fact]
    public void CommonColumns_ContainsAllCriticalMappings()
    {
        // Assert
        AthenaColumnNormalizer.CommonColumns.Mappings.Should().ContainKey("bill/PayerAccountId");
        AthenaColumnNormalizer.CommonColumns.Mappings.Should().ContainKey("lineItem/UsageStartDate");
        AthenaColumnNormalizer.CommonColumns.Mappings.Should().ContainKey("lineItem/UnblendedCost");
        AthenaColumnNormalizer.CommonColumns.Mappings.Should().ContainKey("lineItem/BlendedCost");
        AthenaColumnNormalizer.CommonColumns.Mappings.Should().ContainKey("product/ProductName");
    }

    [Fact]
    public void CommonColumns_AllMappingsAreCorrect()
    {
        // Assert - verify all common mappings follow normalization rules
        foreach (var mapping in AthenaColumnNormalizer.CommonColumns.Mappings)
        {
            var normalized = AthenaColumnNormalizer.Normalize(mapping.Key);
            normalized.Should().Be(mapping.Value,
                $"Mapping for '{mapping.Key}' should normalize to '{mapping.Value}'");
        }
    }

    [Theory]
    [InlineData("identity/LineItemId", "identity_line_item_id")]
    [InlineData("bill/InvoiceId", "bill_invoice_id")]
    [InlineData("lineItem/UsageAccountId", "line_item_usage_account_id")]
    [InlineData("lineItem/ProductCode", "line_item_product_code")]
    [InlineData("pricing/publicOnDemandCost", "pricing_public_on_demand_cost")]
    public void CommonColumns_ContainsMappingAndNormalizesCorrectly(string key, string expectedValue)
    {
        // Assert
        AthenaColumnNormalizer.CommonColumns.Mappings.Should().ContainKey(key);
        AthenaColumnNormalizer.CommonColumns.Mappings[key].Should().Be(expectedValue);

        // Also verify normalization produces same result
        var normalized = AthenaColumnNormalizer.Normalize(key);
        normalized.Should().Be(expectedValue);
    }

    [Fact]
    public void Normalize_AllCommonColumnMappings_MatchDictionary()
    {
        // This test ensures the Normalize method is consistent with CommonColumns dictionary
        foreach (var mapping in AthenaColumnNormalizer.CommonColumns.Mappings)
        {
            // Act
            var normalized = AthenaColumnNormalizer.Normalize(mapping.Key);

            // Assert
            normalized.Should().Be(mapping.Value,
                $"Key '{mapping.Key}' normalized to '{normalized}' but dictionary has '{mapping.Value}'");
        }
    }

    [Theory]
    [InlineData("UPPERCASE", "u_p_p_e_r_c_a_s_e")]  // Each uppercase letter gets underscore prefix
    [InlineData("MixedCase", "mixed_case")]
    [InlineData("camelCase", "camel_case")]
    [InlineData("PascalCase", "pascal_case")]
    public void Normalize_DifferentCasingStyles_ConvertsToLowerSnakeCase(string input, string expected)
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }
}
