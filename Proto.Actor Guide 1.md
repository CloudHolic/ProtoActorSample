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
      - ```Request```를 보낸 Actor에게 답을 할 경우 ```context.Respond```를 사용



### 5. Re-Enter

- https://github.com/CloudHolic/ProtoActorSample/tree/master/ReEnter

- ![ActorConcurrency](https://github.com/CloudHolic/ProtoActorSample/blob/master/Images/ActorConcurrency.png?raw=true)

  - Actor는 기본적으로 자기 자신을 Block시키고 작업을 수행함

  - 만일 오랜 시간이 걸리는 메시지를 받을 경우, 그 시간 동안 다른 메시지를 받을 수 없음

  - ```csharp
    public record Timeout(int Time);
    
    public class WaitingActor : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            var message = context.Message;
            
            if (message is Timeout t)
            {
                Thread.Sleep(TimeSpan.FromSeconds(t.Time));
                Console.WriteLine($"WaitingActor: {t.Time} seconds passed");
            }
            
            return Task.CompletedTask;
        }
    }
    ```

    - 지정한 시간동안 대기하는 Actor
    - ```t = 10```인 메시지와 ```t = 1```인 메시지를 순서대로 받으면 10초 대기 후 1초짜리 메시지를 처리함

- ![ReenterConcurrency](https://github.com/CloudHolic/ProtoActorSample/blob/master/Images/ReenterConcurrency.png?raw=true)

  - ```context.ReenterAfter```를 사용하여 오랜 시간이 걸리는 작업을 분리시키고, 다음 메시지를 받을 수 있게 해줌

  - ```csharp
    public record Timeout(int Time);
    
    public class ReenterActor : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            var message = context.Message;
            
            if (message is Timeout t)
            {
                var waitTask = new Task(() => Thread.Sleep(TimeSpan.FromSeconds(t.Time)));
                context.ReenterAfter(waitTask, () =>
                {
                    Console.WriteLine($"ReenterActor: {t.Time} seconds passed");
                });            
                waitTask.Start();
            }
            
            return Task.CompletedTask;
        }
    }
    ```

    - ```ReenterAfter```를 사용하여 대기하는 작업을 별도의 Task로 분리한 Actor
    - ```t=10```인 메시지와 ```t=1```인 메시지를 순서대로 받으면 1초짜리 메시지가 먼저 작업이 완료됨
  
- Actor는 각각의 상태를 가지기 때문에 Re-Enter를 이용해서 일시적으로 Task를 분리시키면 Race condition이 발생할 수 있다.

  - **가급적 사용하지 않는 것을 권장**




### 6. Router

- https://github.com/CloudHolic/ProtoActorSample/tree/master/Router

- 복수 개의 Actor를 묶어 마치 하나의 Actor처럼 동작하게 하는 형식

- ```Pool``` - 지정된 숫자만큼 Actor를 내부적으로 생성

- ```Group``` - 이미 존재하는 Actor를 묶어서 관리

- Routing 전략

  - Round Robin

    - ```csharp
      using Proto.Router;
      
      var rrPoolProps = system.Root.NewRoundRobinPool(props, 5);
      var rrGroupProps = system.Root.NewRoundRobinGroup(
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props));
      ```

  - Broadcast

    - ```csharp
      using Proto.Router;
      
      var bcPoolProps = system.Root.NewBroadcastPool(props, 5);
      var bcGroupProps = system.Root.NewBroadcastGroup(
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props));
      ```

      - ```Pool``` 혹은 ```Group```에 속한 모든 Actor들에게 메시지를 보냄
      - 한 메시지가 중복 처리될 수 있음

  - Random

    - ```csharp
      using Proto.Router;
      
      var rdPoolProps = system.Root.NewRandomPool(props, 5);
      var rdGroupProps = system.Root.NewRandomGroup(
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props));
      ```

  - Consistent Hashing

    - ```csharp
      using Proto.Router;
      
      var chPoolProps = system.Root.NewConsistentHashPool(props, 5);
      var chGroupProps = system.Root.NewConsistentHashGroup(
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props),
          system.Root.Spawn(props));
      ```

      - Consistent Hashing은 [여기](https://uiandwe.tistory.com/1325)를 참조

- 하나의 Props로 묶여서 관리되기에 생성한 이후로는 단일 Actor에 메시지를 보내듯이 사용

  - ```csharp
    var rrPid = system.Root.Spawn(rrPoolProps);
    system.Root.Send(rrPid, new Hello("Hello"));
    system.Root.Send(rrPid, new Hello("Hello"));
    system.Root.Send(rrPid, new Hello("Hello"));
    system.Root.Send(rrPid, new Hello("Hello"));
    system.Root.Send(rrPid, new Hello("Hello"));
    ```



### 7. Behavior

- https://github.com/CloudHolic/ProtoActorSample/tree/master/Behaviors

- 현재 상태에 따라 Actor의 동작이 달라져야 할 때 사용

- Behavior에는 ```Become```, ```BecomeStacked```, ```UnbecomeStacked```가 존재

  - ```Become``` - 주어진 함수로 Behavior를 세팅. 기존에 세팅된 함수는 지워짐
  - ```BecomeStacked``` - 주어진 함수로 Behavior를 세팅하고 기존에 세팅된 함수를 보존
  - ```UnbecomeStacked``` - 직전에 세팅되었던 함수로 복원

- Example

  - ```csharp
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
            return _behvior.ReceiveAsync(context);
        }
    }
    ```

    - Behavior의 상태를 ```Off```로 설정
    - ```_behavior.ReceiveAsync```를 호출할 때 현재 세팅된 함수를 수행함

  - ```csharp
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
    ```

    - ```On```, ```Off``` 함수
    - ```PressSwitch``` 메시지가 발생할 때마다 서로의 상태로 세팅함



### 8. System Message

- https://github.com/CloudHolic/ProtoActorSample/tree/master/LifeCycle
- 사용자가 정한 메시지 말고도, 특정 상황에서 발생되는 메시지들이 존재
- 이는 ```SystemMessage```라는 이름으로 이미 정의되어 있음
- ```Started``` - Actor가 생성되었을 경우 발생
- ```Restarting``` - Actor가 재시작되기 직전 발생
- ```Stopping``` - Actor가 종료되기 직전 발생
- ```Stopped``` - Actor가 종료된 후 발생
- ```ReceiveTimeout``` - ```context.SetReceiveTimeout```으로 정의된 시간동안 메시지를 받지 못했을 경우 발생



### 9. Supervision

- 각 Actor는 Child Actor를 생성할 수 있음

- Child Actor에서 문제 발생시 이를 Parent Actor에 통보하고, Parent Actor에서 적절한 방식으로 처리함

  - ```Resume``` - Child Actor를 그대로 실행시킴
  - ```Restart``` - Child Actor를 초기화한 후 재시작
  - ```Stop``` - Child Actor를 중지시킨 후 제거
  - ```Escalate``` - Parent Actor 자신도 실패처리

- 이는 ```SupervisorDirective```로 정의되어 있으며, ```WithChildSupervisorStrategy``` 함수로 설정

- 또한 설정한 전략대로 처리할 Child Actor의 범위에 따라 다음의 2가지 방식이 있음

  - ```One-For-One strategy``` - 문제가 생긴 Child Actor 하나에 대해서만 처리
  - ```All-For-One Strategy``` - 다른 모든 Child Actor들도 같이 처리

- Parent Actor는 Child Actor를 감시할 수 있음

  - ```csharp
    context.Watch(pid);
    /* -- */
    context.Unwatch(pid);
    ```

    - Child Actor를 감시하게 되면, Child Actor의 ```Terminated``` 메시지를 받아볼 수 있음.



### 10. EventStream

- Proto.Actor 내부적으로 발생하는 모든 메시지를 구독할 수 있는 시스템

- 일반적으로 사용할 일은 없으나, 디버깅용으로 사용할 수 있음

- ```csharp
  var subscription = system.EventStream.Subscribe<Message>(msg =>
      Console.WriteLine("Message received"));
  /* -- */
  subscription.Unsubscribe();
  ```

  - 여기서 받을 수 있는 메시지는 System Message, 사용자 정의 메시지를 모두 포함

  - 제대로 도착하지 못한 ```Dead Letter``` 또한 이 방법으로는 받을 수 있음

    - ```csharp
      system.EventStream.Subscribe<DeadLetterEvent>(msg =>
          Console.WriteLIne($"Sender: {msg.Sender}, Pid: {msg.Pid}, Message: {msg.Message}"));
      ```