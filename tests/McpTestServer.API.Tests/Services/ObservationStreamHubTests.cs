using FluentAssertions;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Services.Observations;
using Xunit;

namespace McpTestServer.API.Tests.Services;

public sealed class ObservationStreamHubTests
{
    [Fact]
    public void Publish_removes_completed_subscriber_without_unsubscribe()
    {
        var hub = new ObservationStreamHub();
        var reader = hub.Subscribe("caller@example.com", out var writer);
        hub.SubscriberCount.Should().Be(1);

        writer.TryComplete();
        hub.Publish(CreateObservation("caller@example.com"));

        hub.SubscriberCount.Should().Be(0);
        reader.Completion.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void Publish_writes_only_to_the_matching_caller()
    {
        var hub = new ObservationStreamHub();
        var aliceReader = hub.Subscribe("alice@example.com", out _);
        var bobReader = hub.Subscribe("bob@example.com", out _);

        hub.Publish(CreateObservation("alice@example.com"));

        aliceReader.TryRead(out var aliceObservation).Should().BeTrue();
        aliceObservation!.CallerEmail.Should().Be("alice@example.com");
        bobReader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public void Publish_matches_caller_email_case_insensitively()
    {
        var hub = new ObservationStreamHub();
        var reader = hub.Subscribe("Alice@Example.com", out _);

        hub.Publish(CreateObservation("alice@example.com"));

        reader.TryRead(out var observation).Should().BeTrue();
        observation!.CallerEmail.Should().Be("alice@example.com");
    }

    [Fact]
    public void Publish_does_not_fan_out_unattributed_observations()
    {
        var hub = new ObservationStreamHub();
        var reader = hub.Subscribe("alice@example.com", out _);

        hub.Publish(CreateObservation(callerEmail: null));

        reader.TryRead(out _).Should().BeFalse();
        hub.SubscriberCount.Should().Be(1);
    }

    [Fact]
    public void Subscribe_rejects_blank_caller_email()
    {
        var hub = new ObservationStreamHub();
        var act = () => hub.Subscribe("  ", out _);
        act.Should().Throw<ArgumentException>();
    }

    private static ObservationResponse CreateObservation(string? callerEmail) =>
        new(
            1,
            "req-1",
            callerEmail,
            ScenarioIds.Baseline,
            "tools/call",
            "tool",
            10,
            200,
            false,
            null,
            null,
            "2026-07-28",
            DateTime.UtcNow);
}
