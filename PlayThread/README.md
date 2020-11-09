## Key
* 都在 ThreadPool 上调度
* 粗粒度:Parallel
* 细粒度:Task 最常用
* 更细粒度:Thread/ThreadPool

## Lock
* SpinLock vs lock{}(内部是 Monitor)
* SpinWait vs Thread.Sleep

## Task
* Task.AsyncState 取回启动时传入的对象
* Task.Result 会阻塞的
* Task.ContinueWith 会阻塞而 await 不会
* Task.ContinueWith 粒度更细 await 语法简洁
https://www.codeproject.com/Articles/1018071/ContinueWith-Vs-await
* 分离子任务:父任务不等待
* Task.Run 有可能分配到其他线程执行
https://stackoverflow.com/questions/38739403/await-task-run-vs-await-c-sharp
* Unity Job 的 overhead 更少?
https://jacksondunstan.com/articles/4926
* Task.Delay 生成Task,Thread.Sleep 则不会
* System.Timers.Timer 会回调在主线程上,System.Threading.Timer会回调在线程池中
* Wait() Task 时务必用 try catch 包住,会抛出 AggregateException
 

