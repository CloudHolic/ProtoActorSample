using Proto;

var system = new ActorSystem();

var waitingProps = Props.FromProducer(() => new WaitingActor());
var waitingPid = system.Root.Spawn(waitingProps);

var reenterProps = Props.FromProducer(() => new ReenterActor());
var reenterPid = system.Root.Spawn(reenterProps);

system.Root.Send(waitingPid, new Timeout(10));
system.Root.Send(waitingPid, new Timeout(1));

system.Root.Send(reenterPid, new Timeout(10));
system.Root.Send(reenterPid, new Timeout(1));

Console.ReadLine();