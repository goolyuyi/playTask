# 有哪些工具库可以用?

* Parallel
* TPL
* Dataflow
* Thread
* Concurrent Collections
* PLINQ
* ThreadPool

该如何选择库:

* 都在 ThreadPool 上调度
* 粗粒度:Parallel
* 普通粒度:Task 最常用
* 更细粒度:Thread/ThreadPool

## Task 系统

### Tips

* async/await like javascript
* `Task.WhenAll/Task.WhenAny` waiting method
* `Task.ContinueWith` 显式创建
    * Task.ContinueWith 会阻塞而 await 不会
    * Task.ContinueWith 粒度更细 await 语法简洁
    * https://www.codeproject.com/Articles/1018071/ContinueWith-Vs-await
* `TaskCompletionSource` 类似 js Promise resolve/reject

* `Task.ConfigureAwait`
    * 通常不需要设置
    * false 代表不在 task 挂起时保存当前线程,当前调用栈的上下文
    * 速度加快
  
* `Task.Result` 会阻塞
  
* `Task.AsyncState` 取回启动时传入的对象

* 分离子任务:父任务不等待

* Task.Run 有可能分配到其他线程执行 https://stackoverflow.com/questions/38739403/await-task-run-vs-await-c-sharp

* Unity Job 的 overhead 更少?
  https://jacksondunstan.com/articles/4926

* Task.Delay 生成Task,Thread.Sleep 则不会

* System.Timers.Timer 会回调在主线程上,System.Threading.Timer会回调在线程池中

* Wait() Task 时务必用 try catch 包住,会抛出 AggregateException

### 内部构造

以下摘要均出自博文:

* https://devblogs.microsoft.com/dotnet/configureawait-faq/
  ]

#### `SynchronizationContext`:

* 提供一个`Post`方法
* 有多种实现
* 目的在于在当前同步上下文(当前的线程上下文)中执行一段代码(delegate)

```c#
public void DoWork(Action worker, Action completion)
{
    SynchronizationContext sc = SynchronizationContext.Current;
    ThreadPool.QueueUserWorkItem(_ =>
    {
        try { worker(); }
        finally { sc.Post(_ => completion(), null); }
    });
}
```

#### `TaskScheduler`

和`SynchronizationContext`类似,`TaskScheduler`:

* 提供一个`QueueTask`
* `TaskScheduler.Default`默认的实现就是 ThreadPool
* 也可能有其他的实现比如`ConcurrentExclusiveSchedulerPair`
* `TaskScheduler.Current`联合了当前执行Task的线程

```c#
using System;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        var cesp = new ConcurrentExclusiveSchedulerPair();
        Task.Factory.StartNew(() =>
        {
            Console.WriteLine(TaskScheduler.Current == cesp.ExclusiveScheduler);
        }, default, TaskCreationOptions.None, cesp.ExclusiveScheduler).Wait();
    }
}
```    

某些操作只有在同一个上下文中才能执行(比如 UI 操作),否则会报错:

```c#
System.InvalidOperationException: 'The calling thread cannot access this object because a different thread owns it.'
```

只能这样解决

```c#
private static readonly HttpClient s_httpClient = new HttpClient();

private void downloadBtn_Click(object sender, RoutedEventArgs e)
{
    s_httpClient.GetStringAsync("http://example.com/currenttime").ContinueWith(downloadTask =>
    {
        downloadBtn.Content = downloadTask.Result;
    }, TaskScheduler.FromCurrentSynchronizationContext());
}
```

或更直接的

```c#
private static readonly HttpClient s_httpClient = new HttpClient();

private void downloadBtn_Click(object sender, RoutedEventArgs e)
{
    SynchronizationContext sc = SynchronizationContext.Current;
    s_httpClient.GetStringAsync("http://example.com/currenttime").ContinueWith(downloadTask =>
    {
        sc.Post(delegate
        {
            downloadBtn.Content = downloadTask.Result;
        }, null);
    });
}
```

然而 async/await 似乎是一个更自然的做法

```c#
private static readonly HttpClient s_httpClient = new HttpClient();

private async void downloadBtn_Click(object sender, RoutedEventArgs e)
{
    string text = await s_httpClient.GetStringAsync("http://example.com/currenttime");
    downloadBtn.Content = text;
}
```

实际上 await 调用了 Task.GetAwaiter, awaiter 实际上切换了上下文

#### ConfigureAwait

实际上,调用ConfigureAwait(false)会返回一个特殊的 awaiter await这个 awaiter,会直接跳过(前面提到的)切换上下文的操作

有显著的好处是:

* 更快
* 避免死锁

`ConfigureAwait(true)`不需要显著调用的

`ConfigureAwait(false)`能确保 await 后不会切换到之前的上下文吗?

* 不会,比如遇到 await 不需要挂起(等待)的task时

\[其他问题见博文\]

## Thread 系统

https://docs.microsoft.com/zh-cn/dotnet/standard/threading/overview-of-synchronization-primitives

* `System.Threading.Timer`/`System.Timers.Timer` 计时,ThreadPool 上回调
* Interlocked 极快的计数锁
    * 尽可能用这个解决互锁设计
* lock/`Monitor`/`Mutex`(for process)/`SpinLock` 互斥对象
    * lock 内部使用 Monitor
    * 优先使用 lock
    * SpinLock 不会挂起线程
* SpinWait/Sleep 等待
    * Sleep 会挂起线程,而 SpinWait 不会
* `WaitHandle`(base abstract)/`EventWaitHandle`/`AutoResetEvent`/`ManualResetEvent`/`ManualResetEventSlim` 等待信号/发信号
    * WaitHandel 基类
    * EventWaitHandle
    * AutoResetEvent
    * ManualResetEvent
    * ManualResetEventSlim
* `ReaderWriterLockSlim` 读可自由进入,写只能进入一个,读写互斥
* `Semaphore`/`SemaphoreSlim` 进入减一,离开加一,零了无法进入
* `Barrier` 所有都发信号了才继续
* `CountdownEvent` 每发一次信号减一,清零了才继续

## Concurrent Collection

https://docs.microsoft.com/zh-cn/dotnet/standard/collections/thread-safe/

* ConcurrentDictionary<TKey,TValue>
* ConcurrentQueue<T>
* ConcurrentStack<T>
* ConcurrentBag<T>
* BlockingCollection
    * 放入/取出
    * 可设置最大容量
    * 到达最大容量,放入阻塞
    * 空了,取出阻塞
    * 调用CompleteAdding,放入结束, IsCompleted取出结束