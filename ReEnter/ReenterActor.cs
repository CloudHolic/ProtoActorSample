using Proto;

namespace ReEnter;

public record Timeout(int Time);

public class ReenterActor : IActor
{
    public Task ReceiveAsync(IContext context)
    {
        var message = context.Message;

        if (message is Timeout t)
        {
            var waitTask = new Task(() => Thread.Sleep(TimeSpan.FromSeconds(t.Time)));
            context.ReenterAfter(waitTask, () => Console.WriteLine($"ReenterActor: {t.Time} seconds passed"));
            waitTask.Start();
        }

        return Task.CompletedTask;
    }
}