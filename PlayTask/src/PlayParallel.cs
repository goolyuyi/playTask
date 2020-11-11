using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace playTask
{
    public static class PlayParallel
    {
        public static void Play()
        {
            Sum();
            LocalParallel();
            Part();
        }
        
        static void Part()
        {
            var s = Enumerable.Range(0, 100_000_000).ToArray();

            var r = Partitioner.Create(s);
            var ps = r.GetPartitions(8);

            ps[0].MoveNext();
            Console.WriteLine(ps[0].Current);
            ps[0].MoveNext();
            Console.WriteLine(ps[0].Current);

            ps[7].MoveNext();
            Console.WriteLine(ps[7].Current);
            ps[7].MoveNext();
            Console.WriteLine(ps[7].Current);

            ps[2].MoveNext();
            Console.WriteLine(ps[2].Current);
            ps[2].MoveNext();
            Console.WriteLine(ps[2].Current);
        }

        static void LocalParallel()
        {
            int[] nums = Enumerable.Range(0, 100000000).ToArray();
            var cts = new CancellationTokenSource();

            double total = 0;
            object totalLock = new object();
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;

            async Task Function()
            {
                await Task.Delay(5000);
                Console.WriteLine("50ms!");
                cts.Cancel();
            }

            Task.Factory.StartNew(Function, cts.Token);

            try
            {
                //NOTE 进一步加速:
                //初始化线程内部状态=>线程执行=>汇总
                var res = Parallel.For<double>(0, nums.Length,
                    po,
                    () => 0.0,

                    //NOTE 线程action
                    (j, loop, subtotal) =>
                    {
                        //同线程内不用锁
                        subtotal = Math.Sqrt(subtotal + nums[j]);
                        po.CancellationToken.ThrowIfCancellationRequested();
                        return subtotal;
                    },

                    //NOTE 汇总action
                    (x) =>
                    {
                        //NOTE must lock here
                        lock (totalLock)
                        {
                            total += x;
                        }
                    }
                );
                Console.WriteLine("The total is {0:N0}", total);
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine("canceled," + e.Message);
            }
        }

        static void Sum()
        {
            var x = new int[10000 * 10000];

            var rnd = new Random();

            //NOTE 自动执行最优的并行方案

            Parallel.For(0, x.Length, (i) => { x[i] = rnd.Next(10); });

            var res = 0;

            //NOTE profile performance
            //NOTE sometimes it slower than straight
            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            Parallel.For(0, x.Length, (i) =>
            {
                //NOTE DON'T wrong caused by racing
                // res += x[i];

                Interlocked.Add(ref res, x[i]);
            });
            stopwatch.Stop();
            Console.WriteLine($"time consume for Parallel:{stopwatch.Elapsed.Milliseconds}");

            stopwatch.Reset();
            stopwatch.Start();
            var resRight = 0;
            foreach (var i in x)
            {
                resRight += i;
            }

            stopwatch.Stop();
            Console.WriteLine($"time consume for straight:{stopwatch.Elapsed.Milliseconds}");
            Console.WriteLine(resRight);
            Console.WriteLine(res);
        }
    }
}