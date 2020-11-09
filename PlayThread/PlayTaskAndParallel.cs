using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace playCS.PlayParallel
{
    public class PlayTaskAndParallel
    {
        public static void Play()
        {
            // SimpleTask();
            // InputOutputObjAndFactory();
            // CancelIt();
            // ContinueTask连续任务();
            except();
        }


        static void except()
        {
            var task1 = Task<int>.Run(() =>
            {
                Console.WriteLine("Executing task {0}",
                    Task.CurrentId);
                throw new Exception("bad op!");
                return 54;
            });
            var continuation = task1.ContinueWith((antecedent) =>
            {
                Console.WriteLine("task2");
                var e = antecedent.Exception;
                Console.WriteLine("last err:" + e);
                Console.WriteLine("Executing continuation task {0}",
                    Task.CurrentId);
                // Console.WriteLine("Value from antecedent: {0}",
                    // antecedent.Result);
                throw new InvalidOperationException("err2");
            });
            try
            {
                Task.WaitAll(task1, continuation);
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("ae:"+ae.InnerExceptions.Count);
                // Console.WriteLine(task1.Exception);
                // Console.WriteLine(continuation.Exception);
                foreach (var ex in ae.InnerExceptions)
                    Console.WriteLine(ex.Message);
            }
        }

        static void ContinueTask连续任务()
        {
            //also works without Factory
            var t = new Task<string>((obj) =>
            {
                Thread.SpinWait(10000);
                var res = String.Join("+", obj as List<string>);
                Console.WriteLine(res);
                return res;
            }, new List<string>() {"cat", "dog"});

            var t2 = t.ContinueWith((lastTask) =>
            {
                Console.WriteLine("waiting...");
                Console.WriteLine("last is " + lastTask.Result);
            });
            t.Start();

            //See also
            // Task.Factory.ContinueWhenAll()
            // Task.Factory.ContinueWhenAny()

            //wait on the last task of the chain!
            t2.Wait();
        }

        static void CancelIt()
        {
            var cts = new CancellationTokenSource();

            void NewFunction()
            {
                var t = Task.Factory.StartNew(() =>
                    {
                        Console.WriteLine(Task.CurrentId);
                        while (true)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            Task.Delay(100).Wait();
                            Console.WriteLine("waiting..." + DateTime.Now.Millisecond);
                        }
                    }
                    // , cancellationToken: cts.Token
                );
                Console.WriteLine(t.Id);
            }

            NewFunction();
            Task.Delay(2000).Wait();
            cts.Cancel();
        }

        static void InputOutputObjAndFactory()
        {
            var ts = new Task<string>[10];


            for (int i = 0; i < ts.Length; i++)
            {
                ts[i] = Task.Factory.StartNew((object o) =>
                    {
                        Console.WriteLine(Task.CurrentId);
                        var nm = (o as Dictionary<string, string>)["name"];
                        Console.WriteLine(nm);
                        return nm;
                    }
                    ,
                    state: new Dictionary<string, string>()
                    {
                        ["name"] = "mimi:" + "yuyi" + i
                    }
                );
            }

            Task.WaitAll(ts);
            for (var i = 0; i < ts.Length; i++)
            {
                Console.WriteLine(ts[i].Result);
            }
        }

        static void SimpleTask()
        {
            async Task SimpleAction()
            {
                await Task.Delay(1000);
                Console.WriteLine("Task!");
            }

            var t = SimpleAction();
            t.Wait();
            Console.WriteLine("waiting?");

            //To working thread...
            var t2 = Task.Run(SimpleAction);
            t2.Wait();
            Console.WriteLine("waiting?");
        }
    }
}