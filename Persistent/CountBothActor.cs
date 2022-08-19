using Proto;
using Proto.Persistence;
using Proto.Persistence.SnapshotStrategies;

namespace Persistent;

public class CountBothActor : IActor
{
    private int _value;
    private readonly Persistence _persistence;

    public CountBothActor(IProvider provider, string actorId)
    {
        _persistence = Persistence.WithEventSourcingAndSnapshotting(provider, provider, actorId, 
            ApplyEvent, ApplySnapshot, new IntervalStrategy(5), () => _value);
    }

    public async Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Started _:
                await _persistence.RecoverStateAsync();
                break;
            case Add msg:
                if (msg.Amount > 0)
                    await _persistence.PersistEventAsync(new Add { Amount = msg.Amount })
                        .ContinueWith(_ => context.Respond(_value));
                break;
        }
    }

    private void ApplyEvent(Event @event)
    {
        _value = @event.Data switch
        {
            Add msg => _value + msg.Amount,
            _ => _value
        };
    }

    private void ApplySnapshot(Snapshot snapshot)
    {
        if (snapshot.State is int snap)
            _value = snap;
    }

    private static bool ShouldTakeSnapshot() => DateTime.UtcNow.Second % 5 == 0;
}