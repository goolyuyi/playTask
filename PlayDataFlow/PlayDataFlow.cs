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
    public static class PlayDataFlow
    {
        public static void Play()
        {
            MyConcept();
            MyConcept_Choose();
            MyConcept_Compete();
        }

        #region #MyConcept

        private static void MyConcept()
        {
            var srcBuf = new BufferBlock<string>();

            //option means only buffer size is 1 message
            var tarBuf = new BufferBlock<string>(new DataflowBlockOptions() {BoundedCapacity = 1});

            var tarAct = new ActionBlock<string>(s => { Console.WriteLine($"act:{s}"); });

            srcBuf.LinkTo(tarBuf);
            srcBuf.LinkTo(tarAct);

            // NOTE will send to tarBuf and buffered
            srcBuf.Post("aaa");
            // NOTE can't send to tarBuf because buffer is full, so send to tarAct
            srcBuf.Post("bbb");
            srcBuf.Post("ccc");

            Console.WriteLine(tarBuf.Receive());

            //nothing
            Console.WriteLine(tarBuf.TryReceive(out string xx));
            tarBuf.Complete();

            //NOTE will send to tarAct due to tarBuf isCompleted
            srcBuf.Post("ddd");
            //will send to tarAct because tarBuf isCompleted
            Task.Delay(2000).Wait();
        }

        private static void MyConcept_Choose()
        {
            var srcbuf = new BufferBlock<string>();
            var srcbuf2 = new BufferBlock<string>();

            var tarbuf = new BufferBlock<string>();

            var task = DataflowBlock.Choose(srcbuf, async s =>
                {
                    Console.WriteLine($"src1:{s}");
                    srcbuf.LinkTo(tarbuf);
                }, srcbuf2,
                s => { Console.WriteLine($"src2:{s}"); });

            srcbuf.Post("aaaa");
            srcbuf2.Post("bbbb");
            srcbuf.Post("cccc");

            Console.WriteLine(tarbuf.Receive());
            task.Wait();
        }

        private static void MyConcept_Compete()
        {
            var act = new ActionBlock<int>(s =>
            {
                if (s < 0) throw new ArgumentOutOfRangeException();
                Console.WriteLine($"act: {s:N2}");
            });

            act.Post(1);
            act.Post(-2);
            act.Complete();

            try
            {
                act.Completion.Wait();
            }
            catch (AggregateException e)
            {
                Console.WriteLine($"errored: {e.Message}");
            }
        }

        #endregion
    }
}