using System;
using System.Threading;
using System.Threading.Tasks;

namespace playTask
{
    public static class PlayAsyncAwait
    {
        public static void Play()
        {
            AwaitItAsync();
            AwaitIt2Async();
            AwaitIt3Async();
            AwaitIt4Async();
            AwaitIt5Async();

            //guardian
            Task.Delay(2000).Wait();
        }

        //NOTE 用 await 必须申明 async!
        //* 约定命名:[***Async]
        static async void AwaitItAsync()
        {
            async Task Coffee()
            {
                await Task.Delay(1000);
                Console.WriteLine("Coffee");
            }

            await Coffee();
        }


        //NOTE Task<Result>
        static async void AwaitIt2Async()
        {
            async Task<string> ComplexThing(string s)
            {
                await Task.Delay(500);
                return s.Substring(0, 1).ToUpper();
            }

            var res = await ComplexThing("dark");
            Console.WriteLine(res);
        }


        //NOTE use WhenAll!
        static async void AwaitIt3Async()
        {
            async Task M()
            {
                await Task.Delay(800);
                Console.WriteLine("MMM");
            }

            await Task.WhenAll(Task.Delay(1000), M());

            //NOTE This is also sync 
            // Task.WaitAll(Task.Delay(1000), M());

            Console.WriteLine("Done!");
        }


        //Cancel by token
        static async void AwaitIt4Async()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            async Task Do()
            {
                await Task.Delay(10000, cts.Token);
            }

            Task taskDo = default;
            taskDo = Do();
            await Task.Delay(500);
            cts.Cancel();

            //will throw a TaskCanceledException
            try
            {
                await taskDo;
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine(taskDo?.IsCanceled); //TRUE
                Console.WriteLine(taskDo?.IsCompleted); //FALSE
                Console.WriteLine(taskDo?.Status);


                //get a TaskCanceledException here
                Console.WriteLine(e);
            }

            Console.WriteLine(nameof(AwaitIt4Async) + "OKAY");
        }


        static async void AwaitIt5Async()
        {
            int n = 1000 * 1000;

            Func<int> f = () =>
            {
                int res = 0;
                for (var i = 0; i < n; i++)
                {
                    res += 1;
                    Math.Sqrt(res);
                }

                return res;
            };

            //SEND to threadpool
            var t = Task.Run(f);
            Console.WriteLine("here");
            Console.WriteLine("still....");
            Console.WriteLine(await t);
        }
    }
}