using Proto;
using Proto.Router;

var system = new ActorSystem();

var props = Props.FromProducer(() => new HelloActor());

var rrPoolProps = system.Root.NewRoundRobinPool(props, 5);
var rrGroupProps = system.Root.NewRoundRobinGroup(
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props));

var bcPoolProps = system.Root.NewBroadcastPool(props, 5);
var bcGroupProps = system.Root.NewBroadcastGroup(
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props));

var rdPoolProps = system.Root.NewRandomPool(props, 5);
var rdGroupProps = system.Root.NewRandomGroup(
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props));

var chPoolProps = system.Root.NewConsistentHashPool(props, 5);
var chGroupProps = system.Root.NewConsistentHashGroup(
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props),
    system.Root.Spawn(props));

var rrPid = system.Root.Spawn(rrPoolProps);
system.Root.Send(rrPid, new Hello("Hello"));
system.Root.Send(rrPid, new Hello("Hello"));
system.Root.Send(rrPid, new Hello("Hello"));
system.Root.Send(rrPid, new Hello("Hello"));
system.Root.Send(rrPid, new Hello("Hello"));

Console.ReadLine();