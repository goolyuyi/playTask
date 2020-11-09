## 有哪些工具库可以用?

* Dataflow 系统

* Task 系统
    * async/await
    
* [Thread 系统](https://docs.microsoft.com/zh-cn/dotnet/standard/threading/overview-of-synchronization-primitives)
    * `System.Threading.Timer`/`System.Timers.Timer` 计时,ThreadPool 上回调
    * lock/`Monitor`/`Mutex`(for process)/`SpinLock` 互斥对象
    * `WaitHandel`(base abstract)/`EventWaitHandle`/`AutoResetEvent`/`ManualResetEvent`/`ManualResetEventSlim` 等待信号/发信号
    * `ReaderWriterLockSlim` 读可自由进入,写只能进入一个,读写互斥
    * `Semaphore`/`SemaphoreSlim` 进入减一,离开加一,零了无法进入
    * `Barrier` 所有都发信号了才继续
    * `CountdownEvent` 每发一次信号减一,清零了才继续
    * Interlocked 极快的计数
    * SpinWait/Sleep 等待
    
* [Concurrent Collection](https://docs.microsoft.com/zh-cn/dotnet/standard/collections/thread-safe/)
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
         
    
