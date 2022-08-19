using Microsoft.Data.Sqlite;
using Proto;
using Persistent;
using Proto.Persistence.Sqlite;

var system = new ActorSystem();

var eventProps = Props.FromProducer(() =>
    new CountEventActor(
        new SqliteProvider(
            new SqliteConnectionStringBuilder { DataSource = "states.db" }),
        "persistent-event"));

var snapshotProps = Props.FromProducer(() =>
    new CountSnapshotActor(
        new SqliteProvider(
            new SqliteConnectionStringBuilder { DataSource = "states.db" }),
        "persistent-snapshot"));

var bothProps = Props.FromProducer(() =>
    new CountBothActor(
        new SqliteProvider(
            new SqliteConnectionStringBuilder { DataSource = "states.db" }),
        "persistent-both"));

var pid = system.Root.Spawn(bothProps);

system.Root.RequestAsync<int>(pid, new Add { Amount = 1 })
    .ContinueWith(x => Console.WriteLine(x.Result));

Console.ReadLine();