using System.Text;

namespace AwsCurAnonymize.Core;

/// <summary>
/// Normalizes AWS CUR column names to match Athena's naming convention.
/// Based on AWS documentation: https://docs.aws.amazon.com/cur/latest/userguide/cur-ate-run.html
/// </summary>
public static class AthenaColumnNormalizer
{
    /// <summary>
    /// Transforms a CUR column name to Athena's normalized format.
    /// Rules from AWS docs:
    /// 1. Add underscore before uppercase letters
    /// 2. Replace uppercase with lowercase
    /// 3. Replace non-alphanumeric characters with underscore
    /// 4. Remove duplicate underscores
    /// 5. Remove leading/trailing underscores
    /// </summary>
    /// <param name="columnName">Original CUR column name (e.g., "lineItem/UsageStartDate")</param>
    /// <returns>Athena-normalized name (e.g., "line_item_usage_start_date")</returns>
    public static string Normalize(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return columnName;

        var result = new StringBuilder();
        bool lastWasUnderscore = false;

        for (int i = 0; i < columnName.Length; i++)
        {
            char c = columnName[i];

            // Rule 1 & 2: Add underscore before uppercase, then convert to lowercase
            if (char.IsUpper(c))
            {
                // Add underscore before uppercase (except at start)
                if (i > 0 && !lastWasUnderscore && result.Length > 0)
                {
                    result.Append('_');
                    lastWasUnderscore = true;
                }
                result.Append(char.ToLowerInvariant(c));
                lastWasUnderscore = false;
            }
            // Rule 3: Replace non-alphanumeric with underscore
            else if (!char.IsLetterOrDigit(c))
            {
                // Rule 4: Avoid duplicate underscores
                if (!lastWasUnderscore && result.Length > 0)
                {
                    result.Append('_');
                    lastWasUnderscore = true;
                }
            }
            else
            {
                result.Append(char.ToLowerInvariant(c));
                lastWasUnderscore = false;
            }
        }

        // Rule 5: Remove leading/trailing underscores
        var normalized = result.ToString().Trim('_');

        return normalized;
    }

    /// <summary>
    /// Gets the SQL expression to create a normalized column alias.
    /// Handles quoting for columns with special characters.
    /// </summary>
    public static string CreateColumnAlias(string originalName)
    {
        var normalized = Normalize(originalName);

        // If original name contains special chars, quote it
        bool needsQuoting = originalName.Contains('/') ||
                           originalName.Contains(' ') ||
                           originalName.Contains('-');

        var quotedOriginal = needsQuoting ? $"\"{originalName}\"" : originalName;

        return $"{quotedOriginal} AS {normalized}";
    }

    /// <summary>
    /// Common CUR column mappings for quick reference
    /// </summary>
    public static class CommonColumns
    {
        public static readonly Dictionary<string, string> Mappings = new()
        {
            // Identity columns
            { "identity/LineItemId", "identity_line_item_id" },
            { "identity/TimeInterval", "identity_time_interval" },
            
            // Bill columns
            { "bill/InvoiceId", "bill_invoice_id" },
            { "bill/InvoicingEntity", "bill_invoicing_entity" },
            { "bill/BillingEntity", "bill_billing_entity" },
            { "bill/BillType", "bill_bill_type" },
            { "bill/PayerAccountId", "bill_payer_account_id" },
            { "bill/BillingPeriodStartDate", "bill_billing_period_start_date" },
            { "bill/BillingPeriodEndDate", "bill_billing_period_end_date" },
            
            // Line item columns
            { "lineItem/UsageAccountId", "line_item_usage_account_id" },
            { "lineItem/LineItemType", "line_item_line_item_type" },
            { "lineItem/UsageStartDate", "line_item_usage_start_date" },
            { "lineItem/UsageEndDate", "line_item_usage_end_date" },
            { "lineItem/ProductCode", "line_item_product_code" },
            { "lineItem/UsageType", "line_item_usage_type" },
            { "lineItem/Operation", "line_item_operation" },
            { "lineItem/AvailabilityZone", "line_item_availability_zone" },
            { "lineItem/ResourceId", "line_item_resource_id" },
            { "lineItem/UsageAmount", "line_item_usage_amount" },
            { "lineItem/NormalizationFactor", "line_item_normalization_factor" },
            { "lineItem/NormalizedUsageAmount", "line_item_normalized_usage_amount" },
            { "lineItem/CurrencyCode", "line_item_currency_code" },
            { "lineItem/UnblendedRate", "line_item_unblended_rate" },
            { "lineItem/UnblendedCost", "line_item_unblended_cost" },
            { "lineItem/BlendedRate", "line_item_blended_rate" },
            { "lineItem/BlendedCost", "line_item_blended_cost" },
            { "lineItem/LineItemDescription", "line_item_line_item_description" },
            { "lineItem/TaxType", "line_item_tax_type" },
            
            // Product columns
            { "product/ProductName", "product_product_name" },
            { "product/location", "product_location" },
            { "product/locationType", "product_location_type" },
            { "product/instanceType", "product_instance_type" },
            { "product/operatingSystem", "product_operating_system" },
            { "product/region", "product_region" },
            { "product/regionCode", "product_region_code" },
            
            // Pricing columns
            { "pricing/term", "pricing_term" },
            { "pricing/unit", "pricing_unit" },
            { "pricing/publicOnDemandCost", "pricing_public_on_demand_cost" },
            { "pricing/publicOnDemandRate", "pricing_public_on_demand_rate" },
        };
    }
}
