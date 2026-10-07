using System.Threading.Channels;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;

namespace McpTestServer.API.Services.Observations;

public interface IObservationStreamHub
{
    ChannelReader<ObservationResponse> Subscribe(string callerEmail, out ChannelWriter<ObservationResponse> writer);

    void Unsubscribe(ChannelWriter<ObservationResponse> writer);

    void Publish(ObservationResponse observation);
}

public sealed class ObservationStreamHub : IObservationStreamHub
{
    private readonly object _lock = new();
    private readonly List<Subscriber> _subscribers = [];

    internal int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                return _subscribers.Count;
            }
        }
    }

    public ChannelReader<ObservationResponse> Subscribe(string callerEmail, out ChannelWriter<ObservationResponse> writer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callerEmail);

        var channel = Channel.CreateBounded<ObservationResponse>(
            new BoundedChannelOptions(McpObservationConstants.SseChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest,
            });

        writer = channel.Writer;
        var subscriber = new Subscriber(callerEmail, writer);
        lock (_lock)
        {
            _subscribers.Add(subscriber);
        }

        return channel.Reader;
    }

    public void Unsubscribe(ChannelWriter<ObservationResponse> writer)
    {
        lock (_lock)
        {
            _subscribers.RemoveAll(subscriber => ReferenceEquals(subscriber.Writer, writer));
        }

        writer.TryComplete();
    }

    public void Publish(ObservationResponse observation)
    {
        if (string.IsNullOrWhiteSpace(observation.CallerEmail))
        {
            return;
        }

        lock (_lock)
        {
            for (var i = _subscribers.Count - 1; i >= 0; i--)
            {
                var subscriber = _subscribers[i];
                if (!EmailsMatch(subscriber.CallerEmail, observation.CallerEmail))
                {
                    continue;
                }

                if (!subscriber.Writer.TryWrite(observation))
                {
                    _subscribers.RemoveAt(i);
                }
            }
        }
    }

    private static bool EmailsMatch(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private sealed record Subscriber(string CallerEmail, ChannelWriter<ObservationResponse> Writer);
}
