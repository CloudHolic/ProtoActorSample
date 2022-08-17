## Proto.Actor 가이드 1 - Actor

### 1. Actor Model

![ActorModel](https://github.com/CloudHolic/ProtoActorSample/blob/master/Images/ActorModel.png?raw=true)

- Thread를 대신하여 나온, 동시성 처리를 가능하게 하는 모델
- Actor에게 메시지를 보내 그 메시지에 맞는 작업을 수행하게끔 함

![actor](https://github.com/CloudHolic/ProtoActorSample/blob/master/Images/actor.png?raw=true)

- 각각의 Actor는 PID, Mailbox, State, Behavior를 가지며, Child Actor를 생성할 수 있음.
  - PID - 각 Actor를 구분하는 유니크한 ID
  - Mailbox - 메시지 수신함
  - State - Actor 각각의 상태(변수)
  - Behavior - 메시지 및 상태에 따라 수행하는 행동(함수)



### 2. Proto.Actor

- https://github.com/CloudHolic/ProtoActorSample/tree/master/SimpleActor

- ```csharp
  using Proto;
  
  public record Hello(string Msg);
  
  public class HelloActor : IActor
  {
      public Task ReceiveAsync(IContext context)
      {
          var message = context.Message;
  
          if (message is Hello helloMsg)        
              Console.WriteLine($"Hello {helloMsg.Msg}");        
  
          return Task.CompletedTask;
      }
  }
  ```

  - Actor는 ```IActor```를 상속받아서 구현하며, ```IActor```에는 ```public Task ReceiveAsync(IContext context)```만 정의되어 있음
  - ```context.Meessage```를 통해 메시지를 받을 수 있으며, 메시지에 따라 원하는 처리를 수행

- ```csharp
  using Proto;
  
  var system = new ActorSystem();
  var props = Props.FromProducer(() => new HelloActor());
  var pid = system.Root.Spawn(props);
  system.Root.Send(pid, new Hello("Hello"));
  ```

  - Props를 통해 만들어질 Actor의 설정을 정의할 수 있음
  - ```context.Spawn```을 통해 주어진 props대로 Actor를 하나 만들고 그 pid를 리턴받음
  - ```context.Send```를 통해 특정 pid의 Actor로 메시지를 보냄



### 3. Props

- https://github.com/CloudHolic/ProtoActorSample/tree/master/Props

- Props를 통해 Actor의 설정을 다양하게 지정할 수 있음

- ```csharp
  var simpleProps = Props.FromProducer(() => new HelloActor());
  ```

  - Actor 객체를 반환하는 Producer delegate를 통한 생성

- ```csharp
  var funcProps = Props.FromFunc(context =>
  {
      Console.WriteLine($"Received message {context.Message}");
      return Task.Complete;
  });
  ```

  - Actor가 될 람다함수를 통한 생성

- ```csharp
  using Proto.DependencyInjection;
  
  var system = new ActorSystem();
  var diProps = system.DI().PropsFor<HelloActor>();
  ```

  - Dependency Injection을 이용한 생성

- ```csharp
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
  ```

  - 다양한 설정들을 커스터마이징하여 생성
  - ```WithProducer``` - 어떤 Actor를 생성할지 정함
  - ```WithDispatcher``` - Dispatcher를 정함
    - Dispatcher - Actor system의 전체적인 스케줄링을 총괄함
      - ```ThreadPoolDispatcher``` - Thread Pool을 사용해 Task 기반으로 동작
      - ```SynchronousDispatcher``` - Blocking되며 동기적으로 동작
      - ```CurrentSynchronizationContextDispatcher``` - 현재 Thread의 Synchronization Context 상에서 동작
    - Throughput : Mailbox 1개당 최대로 허용되는 메시지의 개수 (Default: 300)
  - ```WithMailbox``` - Mailbox를 정함
    - ```UnboundedMailbox``` - 정해진 상한선이 없음. Dispatcher가 허용하는 한 전부 받아서 저장
    - ```BoundedMailbox``` - Mailbox 자체적인 상한선이 존재함. 메시지 개수가 상한선을 넘을 경우 어떤 메시지를 drop할지도 같이 정함
  - ```WithChildSupervisorStrategy``` - Chilld Actor를 제어하는 기본 방식을 지정
  - ```WithReceiverMiddleware``` - Receiver Middleware를 지정. 여러 개가 정의되어 있으면 순차적으로 실행.
    - Actor가 메시지를 받기 전에 middleware가 수행됨
  - ```WithSenderMiddleware``` - Sender Middleware를 지정. 여러 개가 정의되어 있으면 순차적으로 실행.
    - Actor가 메시지를 보내기 전에 middleware가 수행됨
  - ```WithSpawner``` - Spawner를 정함

- ```csharp
  var pid1 = system.Root.Spawn(props);
  var pid2 = system.Root.SpawnPrefix(props, "prefix");
  var pid3 = system.Root.SpawnNamed(props, "Name");
  var pid4 = system.Root.SpawnNamedSystem(props, "SystemName");
  ```

  - Actor를 생성할 때 Actor의 이름을 지정해줄 수 있음.



### 4. Context

- https://github.com/CloudHolic/ProtoActorSample/tree/master/Context

- Actor의 행동을 context로 제어함

- ```RootContext```와 ```ActorContext```의 두 종류가 있음

  - ```csharp
    var system = new ActorSystem();
    var pid = system.Root.Spawn(props);
    ```

    - ```system.Root```가 Root Context

  - ```csharp
    public class HelloActor : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            var message = context.Message;
            
            if (message is Hello helloMsg)
                Console.WriteLine($"Hello {helloMsg.Msg}");
            
            return Task.CompletedTask;
        }
    }
    ```

    - ```ReceiveAsync```의 인자로 넘어오는 ```context```가 Actor Context

- RootContext, ActorContext 둘 다 수행할 수 있는 행동

  - ```csharp
    var self = context.Self;
    var parent = context.Parent;
    var sender = context.Sender;
    var children = context.Children;
    ```

    - ```Self```, ```Parent```, ```Sender```, ```Children```으로 자기 자신, 부모, Sender, 자식들의 PID를 얻을 수 있음

  - ```csharp
    var pid = context.Spawn(props);
    ```

    - Spawn - 새 Actor를 생성

  - ```csharp
    context.Stop(pid);
    await context.StopAsync(pid);
    /* -- */
    context.Poison(pid);
    await context.PoisonAsync(pid);
    ```

    - ```Stop``` - 해당 Actor를 즉시 종료시킴
    - ```Poison``` - 해당 Actor가 현재 처리 중인 메시지가 끝나면 종료시킴

  - ```csharp
    context.Send(pid, message);
    /* -- */
    context.RequestAsync<Response>(pid, message)
        .ContinueWith(x => Console.WriteLine(x.Result))
        .Wait();
    ```

    - ```Send``` - 메시지를 보냄. 답을 받을 수 없음
    - ```Request``` - 메시지를 보내고 답을 받을 수 있음
      - ```Request```를 보낸 Actor로 답을 할 경우 ```context.Respond```를 사용