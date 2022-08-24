using Proto;

namespace LifeCycle;

public record PlayMovieMessage(string Title, int Id);

public record Recoverable();

public class PlaybackActor : IActor
{
    public PlaybackActor() => Console.WriteLine("Creating a PlaybackActor");

    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Started msg:
                ProcessStartedMessage(msg);
                break;

            case PlayMovieMessage msg:
                ProcessPlayMovieMessage(msg);
                break;

            case Recoverable msg:
                ProcessRecoverableMessage(context, msg);
                break;

            case Stopping msg:
                ProcessStoppingMessage(msg);
                break;
        }

        return Task.CompletedTask;
    }

    private void ProcessStartedMessage(Started msg)
    {
        ColorConsole.WriteLineGreen($"PlaybackActor Started");
    }

    private void ProcessPlayMovieMessage(PlayMovieMessage msg)
    {
        ColorConsole.WriteLineYellow($"PlayMovieMessage {msg.Title} for user {msg.Id}");
    }

    private void ProcessRecoverableMessage(IContext context, Recoverable msg)
    {
        PID child;

        if (context.Children == null || context.Children.Count == 0)
        {
            var props = Props.FromProducer(() => new ChildActor());
            child = context.Spawn(props);
        }
        else
            child = context.Children.First();

        context.Forward(child);
    }

    private void ProcessStoppingMessage(Stopping msg)
    {
        ColorConsole.WriteLineGreen("PlaybackActor Stopping");
    }
}
