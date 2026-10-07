using FluentAssertions;
using McpTestServer.API.Services.Admin;
using Xunit;

namespace McpTestServer.API.Tests.Services;

public sealed class EmailMaskingTests
{
    [Theory]
    [InlineData("alice@acme.com", "***@acme.com")]
    [InlineData("bob@company.example", "***@company.example")]
    public void MaskEmail_masks_local_part(string email, string expected) =>
        EmailMasking.MaskEmail(email).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@domain.com")]
    public void MaskEmail_returns_fallback_for_invalid_values(string? email) =>
        EmailMasking.MaskEmail(email).Should().Be(EmailMasking.MaskedLocalPart);
}
