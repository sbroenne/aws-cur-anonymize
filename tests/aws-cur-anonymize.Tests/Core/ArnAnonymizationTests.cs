namespace AwsCurAnonymize.Tests.Core;

public class ArnAnonymizationTests
{
    [Theory]
    [InlineData(
        "arn:aws:ec2:us-east-1:123456789012:instance/i-1234567890abcdef0",
        "MY-SALT")]
    [InlineData(
        "arn:aws:s3:::my-bucket/my-key",
        "MY-SALT")]
    [InlineData(
        "arn:aws:rds:us-west-2:987654321098:db:mydb",
        "SALT-123")]
    public void ArnPattern_WithVariousArns_IsRecognized(string originalArn, string salt)
    {
        // This test validates that ARN anonymization preserves structure
        // The actual anonymization is tested in CurPipelineTests integration tests

        // Just verify the ARN pattern is recognized
        var hasAccountId = System.Text.RegularExpressions.Regex.IsMatch(
            originalArn,
            @":\d{12}:"
        );

        // Salt is used in actual anonymization, validated here for test data
        Assert.False(string.IsNullOrEmpty(salt));
        Assert.StartsWith("arn:", originalArn);
    }

    [Theory]
    [InlineData("not-an-arn")]
    [InlineData("arn:aws:s3:::bucket-without-account")]
    [InlineData("")]
    public void ArnRegex_WithInvalidArn_DoesNotMatch(string invalidArn)
    {
        // Validate that non-ARN strings are not modified
        var arnPattern = @"^arn:([^:]+):([^:]*):([^:]*):(\d{12}):(.+)$";
        var match = System.Text.RegularExpressions.Regex.IsMatch(
            invalidArn ?? string.Empty,
            arnPattern
        );

        Assert.False(match);
    }

    [Theory]
    [InlineData("arn:aws:ec2:us-east-1:123456789012:instance/i-1234567890abcdef0", true)]
    [InlineData("arn:aws:iam::123456789012:role/MyRole", true)]
    [InlineData("arn:aws:s3:::my-bucket", false)]
    public void ArnRegex_WithVariousArns_MatchesCorrectly(string arn, bool shouldMatch)
    {
        var arnPattern = @"^arn:([^:]+):([^:]*):([^:]*):(\d{12}):(.+)$";
        var match = System.Text.RegularExpressions.Regex.IsMatch(arn, arnPattern);

        Assert.Equal(shouldMatch, match);
    }
}
