using FluentAssertions;
using McpTestServer.API.Scenarios;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

public sealed class ScenarioSessionLifecycleTests
{
    [Fact]
    public void Dispose_disposes_IDisposable_state()
    {
        var state = new TrackingDisposable();
        var session = new ScenarioSession("baseline", "{}", "session-user@example.com", mcpSessionId: null);
        session.SetState(state);

        session.Dispose();

        state.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Factory_registers_session_so_terminate_can_cancel_it()
    {
        var registry = new ScenarioSessionRegistry();
        var factory = new ScenarioSessionFactory(registry);

        var session = factory.GetOrCreate("factory-user@example.com", "baseline", "{}");
        session.McpSessionId.Should().NotBeNullOrWhiteSpace();
        registry.TryGet(session.McpSessionId, out var registered).Should().BeTrue();
        registered.Should().BeSameAs(session);

        registry.TerminateSessionsForCaller("factory-user@example.com");
        session.SessionCancellation.IsCancellationRequested.Should().BeTrue();
    }

    [Theory]
    [InlineData("owner@example.com", true)]
    [InlineData("OWNER@example.com", true)]
    [InlineData("other@example.com", false)]
    [InlineData(null, false)]
    public void BelongsTo_matches_only_the_owning_caller(string? callerEmail, bool expected)
    {
        using var session = new ScenarioSession("baseline", "{}", "owner@example.com", mcpSessionId: null);

        session.BelongsTo(callerEmail).Should().Be(expected);
    }

    [Fact]
    public void Anonymous_session_belongs_only_to_anonymous_callers()
    {
        using var session = new ScenarioSession("baseline", "{}", callerEmail: null, mcpSessionId: null);

        session.BelongsTo(null).Should().BeTrue();
        session.BelongsTo("someone@example.com").Should().BeFalse();
    }

    [Fact]
    public void Registry_caps_session_ids_per_session_and_evicts_the_oldest()
    {
        var registry = new ScenarioSessionRegistry();
        using var session = new ScenarioSession("baseline", "{}", "cap-user@example.com", mcpSessionId: null);

        for (var i = 0; i < ScenarioSessionRegistry.MaxSessionIdsPerSession + 10; i++)
        {
            registry.Register($"client-chosen-{i}", session);
        }

        registry.TryGet("client-chosen-0", out _).Should().BeFalse();
        registry.TryGet($"client-chosen-{ScenarioSessionRegistry.MaxSessionIdsPerSession + 9}", out var latest).Should().BeTrue();
        latest.Should().BeSameAs(session);
        registry.ListForCaller("cap-user@example.com").Should().HaveCount(ScenarioSessionRegistry.MaxSessionIdsPerSession);
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }
}
