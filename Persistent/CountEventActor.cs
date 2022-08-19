using Proto;
using Proto.Persistence;

namespace Persistent;

internal class CountEventActor : IActor
{
    private int _value;
    private readonly Persistence _persistence;

    public CountEventActor(IEventStore eventStore, string actorId)
    {
        _persistence = Persistence.WithEventSourcing(eventStore, actorId, ApplyEvent);
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
}