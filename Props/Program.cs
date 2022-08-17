using Proto;
using Proto.DependencyInjection;
using Proto.Mailbox;
using TestProps;

var system = new ActorSystem();

var simpleProps = Props.FromProducer(() => new HelloActor());

var funcProps = Props.FromFunc(context =>
{
    Console.WriteLine($"Received message {context.Message}");
    return Task.CompletedTask;
});

var diProps = system.DI().PropsFor<HelloActor>();

var detailProps = new Props()
    .WithProducer(() => new HelloActor())
    .WithDispatcher(new ThreadPoolDispatcher { Throughput = 300 })
    .WithMailbox(() => UnboundedMailbox.Create())
    .WithChildSupervisorStrategy(new OneForOneStrategy((who, reason) => SupervisorDirective.Restart, 10, TimeSpan.FromSeconds(10)))
    .WithReceiverMiddleware(
        next => async (c, envelope) =>
        {
            Console.WriteLine($"Receiver middleware 1 enter {c.GetType()}:{c}");
            await next(c, envelope);
            Console.WriteLine($"Receiver middleware 1 exit");
        },
        next => async (c, envelope) =>
        {
            Console.WriteLine($"Receiver middleware 2 enter {c.GetType()}:{c}");
            await next(c, envelope);
            Console.WriteLine($"Receiver middleware 2 exit");
        })
    .WithSenderMiddleware(
        next => async (c, target, envelope) =>
        {
            Console.WriteLine($"Sender middleware 1 enter {c.Message?.GetType()}:{c.Message}");
            await next(c, target, envelope);
            Console.WriteLine($"Sender middleware 1 exit");
        },
        next => async (c, target, envelope) =>
        {
            Console.WriteLine($"Sender middleware 2 enter {c.Message?.GetType()}:{c.Message}");
            await next(c, target, envelope);
            Console.WriteLine($"Sender middleware 2 exit");
        })
    .WithSpawner(Props.DefaultSpawner);

var pid1 = system.Root.Spawn(simpleProps);
var pid2 = system.Root.SpawnPrefix(funcProps, "prefix");
var pid3 = system.Root.SpawnNamed(diProps, "helloActor");
var pid4 = system.Root.SpawnNamedSystem(detailProps, "detailActor");

system.Root.Send(pid1, new Hello("Hello"));

Console.ReadLine();