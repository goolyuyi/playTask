using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PlayDataFlow
{
    public class PlayDataFlow
    {
        public static void Play()
        {
            MyConcept();
            // MyConcept_Choose();
            // MyConcept_Compete();

            // Concept();
            // Concept2();
            // Concept3();

            // TargetToSource();
            // TransformPlay();
            // PlayPipe();
            // PlayCancel();
            // PlayParallel();
            // PlayJoin();
        }


        #region #PlayParallel

        static TimeSpan TimeDataflowComputations(int maxDegreeOfParallelism,
            int messageCount)
        {
            var workerBlock = new ActionBlock<int>(
                millisecondsTimeout => Thread.Sleep(millisecondsTimeout),
                new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = maxDegreeOfParallelism}
            );

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < messageCount; i++)
            {
                //NOTE actionBlock 可以在多个线程上并行执行!
                workerBlock.Post(1000);
            }

            workerBlock.Complete();

            // Wait for all messages to propagate through the network.
            workerBlock.Completion.Wait();

            // Stop the timer and return the elapsed number of milliseconds.
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        private static void PlayParallel()
        {
            int processorCount = Environment.ProcessorCount;
            int messageCount = processorCount;

            Console.WriteLine("Processor count = {0}.", processorCount);

            TimeSpan elapsed;

            elapsed = TimeDataflowComputations(1, messageCount);
            Console.WriteLine("Degree of parallelism = {0}; message count = {1}; " +
                              "elapsed time = {2}ms.", 1, messageCount, (int) elapsed.TotalMilliseconds);

            elapsed = TimeDataflowComputations(processorCount, messageCount);
            Console.WriteLine("Degree of parallelism = {0}; message count = {1}; " +
                              "elapsed time = {2}ms.", processorCount, messageCount, (int) elapsed.TotalMilliseconds);
        }

        #endregion

        #region #PlayCancel

        public static T ReceiveFromAny<T>(params ISourceBlock<T>[] sources)
        {
            var writeOnceBlock = new WriteOnceBlock<T>(e => e);
            foreach (var source in sources)
            {
                source.LinkTo(writeOnceBlock, new DataflowLinkOptions {MaxMessages = 1});
            }

            return writeOnceBlock.Receive();
        }

        static int TrySolution(int n, CancellationToken ct)
        {
            SpinWait.SpinUntil(() => ct.IsCancellationRequested,
                new Random().Next(3000));

            return n + 42;
        }

        private static void PlayCancel()
        {
            var cts = new CancellationTokenSource();

            Func<int, int> action = n => TrySolution(n, cts.Token);
            var trySolution1 = new TransformBlock<int, int>(action);
            var trySolution2 = new TransformBlock<int, int>(action);
            var trySolution3 = new TransformBlock<int, int>(action);

            trySolution1.Post(11);
            trySolution2.Post(21);
            trySolution3.Post(31);

            int result = ReceiveFromAny(trySolution1, trySolution2, trySolution3);

            cts.Cancel();

            Console.WriteLine("The solution is {0}.", result);

            cts.Dispose();
        }

        #endregion

        #region #PlayPipe

        private static void PlayPipe()
        {
            var downloadString = new TransformBlock<string, string>(async uri =>
            {
                Console.WriteLine("Downloading '{0}'...", uri);
                var res = await new HttpClient(new HttpClientHandler
                    {AutomaticDecompression = System.Net.DecompressionMethods.GZip}).GetStringAsync(uri);
                return res;
            });

            var createWordList = new TransformBlock<string, string[]>(text =>
            {
                Console.WriteLine("Creating word list...");

                char[] tokens = text.Select(c => char.IsLetter(c) ? c : ' ').ToArray();
                text = new string(tokens);

                return text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            });

            var filterWordList = new TransformBlock<string[], string[]>(words =>
            {
                Console.WriteLine("Filtering word list...");

                return words
                    .Where(word => word.Length > 3)
                    .Distinct()
                    .ToArray();
            });

            var findReversedWords = new TransformManyBlock<string[], string>(words =>
            {
                Console.WriteLine("Finding reversed words...");

                var wordsSet = new HashSet<string>(words);

                return from word in words.AsParallel()
                    let reverse = new string(word.Reverse().ToArray())
                    where word != reverse && wordsSet.Contains(reverse)
                    select word;
            });

            var printReversedWords = new ActionBlock<string>(reversedWord =>
            {
                Console.WriteLine("Found reversed words {0}/{1}",
                    reversedWord, new string(reversedWord.Reverse().ToArray()));
            });


            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};

            downloadString.LinkTo(createWordList, linkOptions);
            createWordList.LinkTo(filterWordList, linkOptions);
            filterWordList.LinkTo(findReversedWords, linkOptions);
            findReversedWords.LinkTo(printReversedWords, linkOptions);

            downloadString.Post("http://www.gutenberg.org/cache/epub/16452/pg16452.txt");
            downloadString.Complete();
            printReversedWords.Completion.Wait();
        }

        #endregion

        #region #TransformPlay

        private static void TransformPlay()
        {
            async Task<int> CountBytesAsync(string path)
            {
                byte[] buffer = new byte[1024];
                int totalZeroBytesRead = 0;
                using (var fileStream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, true))
                {
                    int bytesRead = 0;
                    do
                    {
                        // Asynchronously read from the file stream.
                        bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                        totalZeroBytesRead += buffer.Count(b => b == 0);
                    } while (bytesRead > 0);
                }

                return totalZeroBytesRead;
            }

            string tempFile = Path.GetTempFileName();

            using (var fileStream = File.OpenWrite(tempFile))
            {
                Random rand = new Random();
                byte[] buffer = new byte[1024];
                for (int i = 0; i < 512; i++)
                {
                    rand.NextBytes(buffer);
                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }

            var printResult = new ActionBlock<int>(zeroBytesRead =>
            {
                Console.WriteLine("{0} contains {1} zero bytes.",
                    Path.GetFileName(tempFile), zeroBytesRead);
            });

            var countBytes = new TransformBlock<string, int>(CountBytesAsync);

            countBytes.LinkTo(printResult);
            countBytes.Completion.ContinueWith(delegate { printResult.Complete(); });

            countBytes.Post(tempFile);
            countBytes.Complete();

            printResult.Completion.Wait();

            File.Delete(tempFile);
        }

        #endregion

        #region TargetToSource

        private static void TargetToSource()
        {
            void Produce(ITargetBlock<byte[]> target)
            {
                Random rand = new Random();

                for (int i = 0; i < 100; i++)
                {
                    byte[] buffer = new byte[1024];
                    rand.NextBytes(buffer);

                    target.Post(buffer);
                }

                target.Complete();
            }

            async Task<int> ConsumeAsync(ISourceBlock<byte[]> source)
            {
                int bytesProcessed = 0;

                while (await source.OutputAvailableAsync())
                {
                    byte[] data = source.Receive();
                    bytesProcessed += data.Length;
                }

                return bytesProcessed;
            }

            var buffer = new BufferBlock<byte[]>();

            var consumer = ConsumeAsync(buffer);
            Produce(buffer);
            consumer.Wait();

            Console.WriteLine("Processed {0} bytes.", consumer.Result);
        }

        #endregion


        #region #JoinBlock

        abstract class Resource
        {
        }

        class MemoryResource : Resource
        {
        }

        class NetworkResource : Resource
        {
        }

        class FileResource : Resource
        {
        }

        private static void PlayJoin()
        {
            var networkResources = new BufferBlock<NetworkResource>();
            var fileResources = new BufferBlock<FileResource>();
            var memoryResources = new BufferBlock<MemoryResource>();
            
            var joinNetworkAndMemoryResources =
                new JoinBlock<NetworkResource, MemoryResource>(
                    new GroupingDataflowBlockOptions
                    {
                        Greedy = false
                    });

            var joinFileAndMemoryResources =
                new JoinBlock<FileResource, MemoryResource>(
                    new GroupingDataflowBlockOptions
                    {
                        Greedy = false
                    });

            var networkMemoryAction = new ActionBlock<Tuple<NetworkResource, MemoryResource>>(
                data =>
                {
                    Console.WriteLine("Network worker: using resources...");
                    Thread.Sleep(new Random().Next(500, 2000));

                    Console.WriteLine("Network worker: finished using resources...");

                    networkResources.Post(data.Item1);
                    memoryResources.Post(data.Item2);
                });

            var fileMemoryAction =
                new ActionBlock<Tuple<FileResource, MemoryResource>>(
                    data =>
                    {
                        Console.WriteLine("File worker: using resources...");

                        Thread.Sleep(new Random().Next(500, 2000));

                        Console.WriteLine("File worker: finished using resources...");

                        fileResources.Post(data.Item1);
                        memoryResources.Post(data.Item2);
                    });

            networkResources.LinkTo(joinNetworkAndMemoryResources.Target1);
            memoryResources.LinkTo(joinNetworkAndMemoryResources.Target2);

            fileResources.LinkTo(joinFileAndMemoryResources.Target1);
            memoryResources.LinkTo(joinFileAndMemoryResources.Target2);


            joinNetworkAndMemoryResources.LinkTo(networkMemoryAction);
            joinFileAndMemoryResources.LinkTo(fileMemoryAction);

            networkResources.Post(new NetworkResource());
            memoryResources.Post(new MemoryResource());
            networkResources.Post(new NetworkResource());
            networkResources.Post(new NetworkResource());

            // memoryResources.Post(new MemoryResource());

            // fileResources.Post(new FileResource());
            // fileResources.Post(new FileResource());
            // fileResources.Post(new FileResource());

            Thread.Sleep(10000);
        }

        #endregion


        #region #MyConcept

        private static void MyConcept()
        {
            var srcBuf = new BufferBlock<string>();
            var tarBuf = new BufferBlock<string>();

            var tarOutput = new ActionBlock<string>(s => { Console.WriteLine($"tar2:{s}"); });

            bool LengthPredicate(string s)
            {
                return s.Length >= 5;
            }

            //NOTE dispose this to breaking the link
            using (srcBuf.LinkTo(tarBuf))
            {
                //NOTE input
                srcBuf.Post("youtrack");
                //NOTE output
                Console.WriteLine(tarBuf.Receive());
            }

            var transformBroadcast = new BroadcastBlock<string>(null);

            //NOTE tarOutput<--transformBroadcast
            //NOTE srcBuf<--transformBroadcastd
            transformBroadcast.LinkTo(tarOutput, LengthPredicate);
            transformBroadcast.LinkTo(tarBuf, LengthPredicate);
            transformBroadcast.Post("you");
            transformBroadcast.Post("youtrack");

            Console.WriteLine(tarBuf.Receive());
        }

        private static void MyConcept_Choose()
        {
            //NOTE 这里 BufferBlock 即是 source 也是 target
            var buf = new BufferBlock<string>();

            var buf2 = new BufferBlock<string>();

            //NOTE Choose 监听源
            //NOTE 对于两个源,谁先有消息谁先执行
            var task = DataflowBlock.Choose(buf, async s => { Console.WriteLine($"src1:{s}"); }, buf2,
                s => { Console.WriteLine($"src2:{s}"); });

            buf.Post("aaaaaa");
            buf2.Post("bbbbb");
            task.Wait();
        }

        private static void MyConcept_Compete()
        {
            var srcBuf = new BufferBlock<string>();
            var tarOutput = new ActionBlock<string>(s => { Console.WriteLine($"tar2:{s}"); });

            srcBuf.LinkTo(tarOutput);
            srcBuf.Post("aaaaaa");

            srcBuf.Complete();

            //NOTE source 中可能会抛错
            try
            {
                tarOutput.Completion.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region #Concept

        private static void Concept2()
        {
            var bufferBlock = new BufferBlock<int>();

            var post = Task.Run(() =>
            {
                Console.WriteLine(
                    $"Task.CurrentId:{Task.CurrentId},Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
                bufferBlock.Post(0);
                bufferBlock.Post(1);
            });

            var receive = Task.Run(() =>
            {
                Console.WriteLine(
                    $"Task.CurrentId:{Task.CurrentId},Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
                for (int i = 0; i < 3; i++)
                {
                    Console.WriteLine(bufferBlock.Receive());
                }
            });

            var post2 = Task.Run(() =>
            {
                Console.WriteLine(
                    $"Task.CurrentId:{Task.CurrentId},Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
                bufferBlock.Post(2);
            });

            Task.WaitAll(post, receive, post2);
        }


        private static void Concept()
        {
            var bufferBlock = new BufferBlock<int>();
            for (int i = 0; i < 3; i++)
            {
                bufferBlock.Post(i);
            }

            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine(bufferBlock.Receive());
            }

            for (int i = 0; i < 3; i++)
            {
                bufferBlock.Post(i);
            }

            int value;
            while (bufferBlock.TryReceive(out value))
            {
                Console.WriteLine(value);
            }
        }

        #region Concept3

        private static async Task AsyncSendReceive(BufferBlock<int> bufferBlock)
        {
            for (int i = 0; i < 3; i++)
            {
                await bufferBlock.SendAsync(i);
            }

            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine(await bufferBlock.ReceiveAsync());
            }
        }

        private static void Concept3()
        {
            var bufferBlock = new BufferBlock<int>();

            //NOTE Demonstrate asynchronous dataflow operations.
            AsyncSendReceive(bufferBlock).Wait();
        }

        #endregion

        #endregion
    }
}