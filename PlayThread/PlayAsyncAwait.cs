using System;
using System.Threading;
using System.Threading.Tasks;

namespace playCS.playThreadsWorld
{
    public class PlayAsyncAwait
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

        //* 用 await 必须申明 async!
        //* 约定***Async
        static async void AwaitItAsync()
        {
            async Task Coffee()
            {
                await Task.Delay(1000);
                Console.WriteLine("Coffee");
            }

            await Coffee();
        }


        //* Task / Task<Result>
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


        static async void AwaitIt3Async()
        {
            async Task M()
            {
                await Task.Delay(800);
                Console.WriteLine("MMM");
            }

            await Task.WhenAll(Task.Delay(1000), M());

            //This is sync version
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

            Do();
            await Task.Delay(500);
            cts.Cancel();
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
                }
                ;

            //SEND to threadpool
            var t = Task.Run(f);
            Console.WriteLine("here");
            Console.WriteLine("still....");
            Console.WriteLine(await t);
        }
    }
}