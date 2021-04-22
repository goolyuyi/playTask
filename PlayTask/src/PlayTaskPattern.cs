using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace playTask
{
    public class PlayTaskPattern
    {
        public static void Play()
        {
            RetryTask();
            Interleaved();
            WhenAllOrFirstException();
            SameAsDelay();
            PlayAsyncCache();
            //NOTE MUST SEE more pattern here
            //https://docs.microsoft.com/zh-cn/dotnet/standard/asynchronous-programming-patterns/consuming-the-task-based-asynchronous-pattern
        }


        // static void NeedOnlyOne()
        // {
        //     async Task<T> needOnlyOne<T>(
        //         params Func<CancellationToken, Task<T>>[] functions)
        //     {
        //         var cts = new CancellationTokenSource();
        //         var tasks = (from function in functions
        //             select function(cts.Token)).ToArray();
        //
        //         var completed = await Task.WhenAny(tasks).ConfigureAwait(false);
        //         cts.Cancel();
        //
        //         foreach (var task in tasks)
        //         {
        //             var ignored = task.ContinueWith(
        //                 t => Log(t), TaskContinuationOptions.OnlyOnFaulted);
        //         }
        //
        //         return completed;
        //     }
        //
        //     // needOnlyOne(Task.Delay);
        // }
        //

        static IEnumerable<Task<string>> genTasks()
        {
            var rnd = new Random();

            async Task<string> EchoDelay(string msg)
            {
                await Task.Delay(rnd.Next(500, 1500));
                return msg;
            }

            return new List<Task<string>>()
            {
                EchoDelay("Hello!"),
                EchoDelay("Yalo!"),
                EchoDelay("OHAIYO!"),
            };
        }

        static void RetryTask()
        {
            async Task<T> RetryOnFault<T>(
                Func<Task<T>> function,
                int maxTries,
                Func<Task> retryWhen
            )
            {
                for (int i = 0; i < maxTries; i++)
                {
                    try
                    {
                        //NOTE ConfigureAwait 不在 Task 中保存当前调用上下文
                        return await function().ConfigureAwait(false);
                    }
                    catch
                    {
                        if (i == maxTries - 1) throw;
                    }

                    await retryWhen().ConfigureAwait(false);
                }

                return default(T);
            }

            try
            {
                var retryTask = RetryOnFault(async () =>
                {
                    await Task.Delay(1000);
                    throw new Exception("aaaa");
                    return "AAA";
                }, 3, () => Task.Delay(1000));

                Console.WriteLine(retryTask.Result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Interleaved()
        {
            static IEnumerable<Task<T>> _Interleaved<T>(IEnumerable<Task<T>> tasks)
            {
                var inputTasks = tasks.ToList();
                var sources = (from _ in Enumerable.Range(0, inputTasks.Count)
                    select new TaskCompletionSource<T>()).ToList();
                int nextTaskIndex = -1;

                foreach (var inputTask in inputTasks)
                {
                    inputTask.ContinueWith(completed =>
                        {
                            var source = sources[Interlocked.Increment(ref nextTaskIndex)];
                            if (completed.IsFaulted)
                                source.TrySetException(completed.Exception.InnerExceptions);
                            else if (completed.IsCanceled)
                                source.TrySetCanceled();
                            else
                                source.TrySetResult(completed.Result);
                        }, CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                return from source in sources
                    select source.Task;
            }

            async Task sample()
            {
                //think about it...
                var iTasks = _Interleaved(genTasks());

                foreach (var t in iTasks)
                {
                    Console.WriteLine(await t);
                }

                //NOTE similar like WhenAny
                //think about it...
                await iTasks.ToArray()[0];
            }

            sample();
            Task.Delay(2000).Wait();
        }

        static void WhenAllOrFirstException()
        {
            Task<T[]> _WhenAllOrFirstException<T>(IEnumerable<Task<T>> tasks)
            {
                var inputs = tasks.ToList();
                var ce = new CountdownEvent(inputs.Count);
                var tcs = new TaskCompletionSource<T[]>();

                Action<Task> onCompleted = (Task completed) =>
                {
                    if (completed.IsFaulted)
                        tcs.TrySetException(completed.Exception.InnerExceptions);
                    if (ce.Signal() && !tcs.Task.IsCompleted)
                        tcs.TrySetResult(inputs.Select(t => t.Result).ToArray());
                };

                foreach (var t in inputs) t.ContinueWith(onCompleted);
                return tcs.Task;
            }

            async Task sample()
            {
                await _WhenAllOrFirstException(genTasks());
                //DO: Cancel other tasks
                //DO: Filter results from tasks
            }

            sample().Wait();
        }


        //NOTE manually op a task
        static void SameAsDelay()
        {
            Task<DateTimeOffset> _Delay(int millisecondsTimeout)
            {
                //NOTE if you want to MANUALLY make a Task , use this
                TaskCompletionSource<DateTimeOffset> tcs = null;
                Timer timer = null;

                timer = new Timer(delegate
                {
                    timer.Dispose();
                    tcs.TrySetResult(DateTimeOffset.UtcNow);
                }, null, Timeout.Infinite, Timeout.Infinite);

                tcs = new TaskCompletionSource<DateTimeOffset>(timer);

                //NOTE for accuracy
                timer.Change(millisecondsTimeout, Timeout.Infinite);

                //NOTE also use for shortcut and simplify
                // return Task.FromResult<DateTimeOffset>(DateTimeOffset.Now);
                return tcs.Task;
            }

            _Delay(1000).Wait();
            Console.WriteLine("Done");
        }


        public class AsyncCache<TKey, TValue>
        {
            private readonly Func<TKey, Task<TValue>> _valueFactory;
            private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _map;

            public AsyncCache(Func<TKey, Task<TValue>> valueFactory)
            {
                _valueFactory = valueFactory ?? throw new ArgumentNullException("loader");
                _map = new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>();
            }

            public Task<TValue> this[TKey key]
            {
                get
                {
                    if (key == null) throw new ArgumentNullException("key");
                    return _map.GetOrAdd(key, toAdd =>
                            new Lazy<Task<TValue>>(() => _valueFactory(toAdd)))
                        .Value;
                }
            }
        }

        static void PlayAsyncCache()
        {
            async Task _Async()
            {
                AsyncCache<string, string> ac = new AsyncCache<string, string>(async (url) =>
                {
                    await Task.Delay(1000);
                    return url.ToUpper();
                });

                //NOTE 直到此刻才会真正调用async factory 计算结果
                var x = await ac["ababab"];
                Console.WriteLine(x);

                //NOTE 且下次访问会缓存结果
                Console.WriteLine(await ac["ababab"]);
            }

            _Async().Wait();
        }
    }

    public class AsyncProducerConsumerCollection<T>
    {
        private readonly Queue<T> m_collection = new Queue<T>();

        private readonly Queue<TaskCompletionSource<T>> m_waiting =
            new Queue<TaskCompletionSource<T>>();

        public void Add(T item)
        {
            TaskCompletionSource<T> tcs = null;
            lock (m_collection)
            {
                if (m_waiting.Count > 0) tcs = m_waiting.Dequeue();
                else m_collection.Enqueue(item);
            }

            if (tcs != null) tcs.TrySetResult(item);
        }

        public Task<T> Take()
        {
            lock (m_collection)
            {
                if (m_collection.Count > 0)
                {
                    return Task.FromResult(m_collection.Dequeue());
                }
                else
                {
                    var tcs = new TaskCompletionSource<T>();
                    m_waiting.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }
    }
}