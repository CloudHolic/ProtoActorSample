using Proto;

namespace ReEnter;
public class WaitingActor : IActor
{
    public Task ReceiveAsync(IContext context)
    {
        var message = context.Message;

        if (message is Timeout t)
        {
            Thread.Sleep(TimeSpan.FromSeconds(t.Time));
            Console.WriteLine($"WaitingActor: {t.Time} seconds passed");
        }

        return Task.CompletedTask;
    }
}