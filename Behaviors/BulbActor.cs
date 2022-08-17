using Proto;

public class BulbActor : IActor
{
    private readonly Behavior _behavior;

    public BulbActor()
    {
        _behavior = new Behavior();
        _behavior.Become(Off);
    }

    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case HitWithHammer _:
                context.Respond("Smashed");
                _behavior.Become(Smashed);
                return Task.CompletedTask;
        }

        return _behavior.ReceiveAsync(context);
    }

    private Task On(IContext context)
    {
        if (context.Message is IBulbAction action)
        {
            switch (action)
            {
                case PressSwitch _:
                    context.Respond("Turning off");
                    _behavior.Become(Off);
                    break;
                case Touch _:
                    context.Respond("Hot!");
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private Task Off(IContext context)
    {
        if (context.Message is IBulbAction action)
        {
            switch (action)
            {
                case PressSwitch _:
                    context.Respond("Turning on");
                    _behavior.Become(On);
                    break;
                case Touch _:
                    context.Respond("Cold");
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private Task Smashed(IContext context)
    {
        if (context.Message is IBulbAction action)
        {
            switch (action)
            {
                case PressSwitch _:
                    context.Respond("");
                    break;
                case Touch _:
                    context.Respond("Owwwww!");
                    break;
                case ReplaceBulb _:
                    _behavior.Become(Off);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
