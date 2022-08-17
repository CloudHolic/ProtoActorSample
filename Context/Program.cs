using Proto;
using Context;

var system = new ActorSystem();

// system.Root = RootContext
var props = Props.FromProducer(() => new HelloActor());
var pid = system.Root.Spawn(props);

system.Root.Stop(pid);
await system.Root.StopAsync(pid);

system.Root.Poison(pid);
await system.Root.PoisonAsync(pid);

system.Root.Send(pid, new Hello("HelloSend"));
system.Root.Request(pid, new Hello("HelloRequest"));
system.Root.RequestAsync<Response>(pid, new Hello("HelloRequestAsync"))
    .ContinueWith(x => Console.WriteLine(x.Result.Msg))
    .Wait();

Console.ReadLine();