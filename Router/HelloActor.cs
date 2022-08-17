using Proto;

public record Hello(string Msg);

public class HelloActor : IActor
{
    public Task ReceiveAsync(IContext context)
    {
        var message = context.Message;

        if (message is Hello helloMsg)
            Console.WriteLine($"Hello({context.Self.Id}): {helloMsg.Msg}");

        return Task.CompletedTask;
    }
}