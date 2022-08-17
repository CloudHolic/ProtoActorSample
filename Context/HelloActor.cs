using Proto;

namespace Context;

public record Hello(string Msg);

public record Response(string Msg);

public class HelloActor : IActor
{
    // context = ActorContext
    public Task ReceiveAsync(IContext context)
    {
        var message = context.Message;

        if (message is Hello helloMsg)
        {
            Console.WriteLine($"Hello {helloMsg.Msg}");
            context.Respond(new Response($"Respond {helloMsg.Msg}"));

            if (helloMsg.Msg == "Create")
            {
                var props = Props.FromProducer(() => new HelloActor());
                var pid = context.Spawn(props);

                context.Send(pid, "Child");
                context.Poison(pid);
            }            
        }

        return Task.CompletedTask;
    }
}