using Proto;
using SimpleActor;

var system = new ActorSystem();

var props = Props.FromProducer(() => new HelloActor());

var pid = system.Root.Spawn(props);

system.Root.Send(pid, new Hello("Hello"));

Console.ReadLine();