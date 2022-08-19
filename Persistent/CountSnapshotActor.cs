using Proto;
using Proto.Persistence;

namespace Persistent;

public class CountSnapshotActor : IActor
{
    private int _value;
    private readonly Persistence _persistence;

    public CountSnapshotActor(ISnapshotStore snapshotStore, string actorId)
    {
        _persistence = Persistence.WithSnapshotting(snapshotStore, actorId, ApplySnapshot);
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
                {
                    _value += msg.Amount;
                    await _persistence.PersistSnapshotAsync(_value)
                        .ContinueWith(t => context.Respond(_value));
                }
                break;
        }
    }

    private void ApplySnapshot(Snapshot snapshot)
    {
        if (snapshot.State is int snap)
            _value = snap;
    }
}