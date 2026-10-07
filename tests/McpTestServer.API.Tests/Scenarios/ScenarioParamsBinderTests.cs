using FluentAssertions;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Scenarios.Errors;
using McpTestServer.API.Scenarios.Timeout;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

public sealed class ScenarioParamsBinderTests
{
    [Fact]
    public void TryBind_rejects_null_collection_properties()
    {
        var bound = ScenarioParamsBinder.TryBind<HttpStatusSequenceParams>(
            """{"statusCodes":null}""",
            out var value,
            out var error);

        bound.Should().BeFalse();
        value.Should().BeNull();
        error.Should().Contain("statusCodes");
    }

    [Fact]
    public void TryBind_rejects_negative_hang_duration()
    {
        var bound = ScenarioParamsBinder.TryBind<TimeoutUncooperativeHangParams>(
            """{"hangOnInvocation":1,"hangDurationMs":-1}""",
            out var value,
            out var error);

        bound.Should().BeFalse();
        value.Should().BeNull();
        error.Should().Be(TimeoutUncooperativeHangParams.HangDurationMsMustBeNonNegative);
    }

    [Fact]
    public void TryBind_accepts_zero_hang_duration()
    {
        var bound = ScenarioParamsBinder.TryBind<TimeoutUncooperativeHangParams>(
            """{"hangOnInvocation":1,"hangDurationMs":0}""",
            out var value,
            out var error);

        bound.Should().BeTrue();
        error.Should().BeNull();
        value!.HangDurationMs.Should().Be(0);
    }
}
