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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_EmptyOrWhitespace_ReturnsInput(string input)
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Normalize_Null_ReturnsNull()
    {
        // Act
        var result = AthenaColumnNormalizer.Normalize(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Normalize_LeadingUnderscore_RemovesIt()
    {
        // Arrange
        var input = "_column/Name";

        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        Assert.False(result.StartsWith("_"));
        Assert.Equal("column_name", result);
    }

    [Fact]
    public void Normalize_TrailingUnderscore_RemovesIt()
    {
        // Arrange
        var input = "column/Name_";

        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        Assert.False(result.EndsWith("_"));
        Assert.Equal("column_name", result);
    }

    [Fact]
    public void Normalize_ConsecutiveSpecialChars_ConsolidatesToSingleUnderscore()
    {
        // Arrange
        var input = "column//Name";

        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        Assert.DoesNotContain("__", result);
        Assert.Equal("column_name", result);
    }

    [Fact]
    public void Normalize_NumbersInColumnName_PreservesNumbers()
    {
        // Arrange
        var input = "product/max32IopsVolume";

        // Act
        var result = AthenaColumnNormalizer.Normalize(input);

        // Assert
        Assert.Contains("32", result);
        Assert.Equal("product_max32_iops_volume", result);
    }

    [Fact]
    public void CreateColumnAlias_WithForwardSlash_QuotesOriginalName()
    {
        // Arrange
        var input = "lineItem/UsageStartDate";

        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias(input);

        // Assert
        Assert.StartsWith("\"lineItem/UsageStartDate\"", result);
        Assert.Contains(" AS ", result);
        Assert.EndsWith("line_item_usage_start_date", result);
    }

    [Fact]
    public void CreateColumnAlias_WithSpaces_QuotesOriginalName()
    {
        // Arrange
        var input = "column name";

        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias(input);

        // Assert
        Assert.StartsWith("\"column name\"", result);
        Assert.Contains(" AS ", result);
        Assert.EndsWith("column_name", result);
    }

    [Fact]
    public void CreateColumnAlias_WithHyphen_QuotesOriginalName()
    {
        // Arrange
        var input = "column-name";

        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias(input);

        // Assert
        Assert.StartsWith("\"column-name\"", result);
        Assert.Contains(" AS ", result);
        Assert.EndsWith("column_name", result);
    }

    [Fact]
    public void CreateColumnAlias_SimpleColumnName_DoesNotQuote()
    {
        // Arrange
        var input = "simplecolumn";

        // Act
        var result = AthenaColumnNormalizer.CreateColumnAlias(input);

        // Assert
        Assert.DoesNotContain("\"", result);
        Assert.Equal("simplecolumn AS simplecolumn", result);
    }

    [Fact]
    public void CommonColumns_ContainsAllCriticalMappings()
    {
        // Assert
        Assert.True(AthenaColumnNormalizer.CommonColumns.Mappings.ContainsKey("bill/PayerAccountId"));
        Assert.True(AthenaColumnNormalizer.CommonColumns.Mappings.ContainsKey("lineItem/UsageStartDate"));
        Assert.True(AthenaColumnNormalizer.CommonColumns.Mappings.ContainsKey("lineItem/UnblendedCost"));
        Assert.True(AthenaColumnNormalizer.CommonColumns.Mappings.ContainsKey("lineItem/BlendedCost"));
        Assert.True(AthenaColumnNormalizer.CommonColumns.Mappings.ContainsKey("product/ProductName"));
    }

    [Fact]
    public void CommonColumns_AllMappingsAreCorrect()
    {
        // Assert - verify all common mappings follow normalization rules
        foreach (var mapping in AthenaColumnNormalizer.CommonColumns.Mappings)
        {
            var normalized = AthenaColumnNormalizer.Normalize(mapping.Key);
            Assert.Equal(mapping.Value, normalized);
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
        Assert.True(AthenaColumnNormalizer.CommonColumns.Mappings.ContainsKey(key));
        Assert.Equal(expectedValue, AthenaColumnNormalizer.CommonColumns.Mappings[key]);

        // Also verify normalization produces same result
        var normalized = AthenaColumnNormalizer.Normalize(key);
        Assert.Equal(expectedValue, normalized);
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
            Assert.Equal(mapping.Value, normalized);
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
        Assert.Equal(expected, result);
    }
}
