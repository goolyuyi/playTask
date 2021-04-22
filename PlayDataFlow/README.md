## 概念

Dataflow 是一个管道系统(类似于 js 的 pipe)!

* Source:
    * link to target

* Target:
    * has `Post()` used by Source or programmatically

* IPropagatorBlock: Input & Output

## Link

* Link 方向:Source -> Target
* Link 操作是线程安全的
* LinkTo 可指定 Predicate<> 功能就是 Filter

## Postponed

1. src call target by Target.OfferMessage

2. Target.OfferMessage returns:
    * Accept
    * Declined
    * DecliningPermanently - will break link by default impl
    * Postponed

3. if Postponed:
    * Target call Src.ReserveMessage and returns:
        * True: Reserved
        * False

    * and Target wanna going on, Target call Src.
        * ConsumeMessage: get back on
        * ReleaseReservation: give up

## Blocks:

### I/O:

* BufferBlock:
    * FIFO queue
    * 连入连出,单入/单出

* BroadcastBlock
    * 连入连出,单入->克隆广播

* WriteOnceBlock
    * 连入连出,单入->克隆广播
    * input once

### Action:

**可并行,可指定并行度**

* ActionBlock
    * 连入,单入
    * `Action` or `System.Func<TInput, Task>(auto await)`
* TransformBlock
    * 连入连出,单入/单出
    * `System.Func<TInput, TOutput>` or `System.Func<TInput, Task<TOutput>>`
* TransformManyBlock
    * 连入连出,单入/分裂
    * `System.Func<TInput, IEnumerable<TOutput>>` or `System.Func<TInput, Task<IEnumerable<TOutput>>>`

### Struct

```
贪婪模式:
* on: 接受它提供的每条消息，并在接收指定数量的元素后传播数组
    * 开销少
* off: 对象推迟所有传入的消息,直到足够的源给块提供消息来形成批
    * 安全
```

* BufferBlock
    * 连入连出,等待一定数量消息/合并单出数组
    * 贪婪模式
* JoinBlock
    * 连入连出,等待所有连入可用/合并为 Tuple 传出
* BatchedJoinBlock
    * 连入连出,等待所有连入可用且达到数量/合并为 Tuple+IList 传出
     
    