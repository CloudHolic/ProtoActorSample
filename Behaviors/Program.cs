using Proto;
using Behaviors;

var system = new ActorSystem();

var props = Props.FromProducer(() => new BulbActor());

var pid = system.Root.Spawn(props);

system.Root.RequestAsync<string>(pid, new PressSwitch())
    .ContinueWith(x => Console.WriteLine(x.Result))
    .Wait();

system.Root.RequestAsync<string>(pid, new PressSwitch())
    .ContinueWith(x => Console.WriteLine(x.Result))
    .Wait();

Console.ReadLine();