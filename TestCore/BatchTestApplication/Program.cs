using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BatchTestApplication
{
    class Program
    {
        static string imagePath = @"1.jpg";
        static void Main(string[] args)
        {
            submitWork();
        }


        private static void submitWork()
        {
            //var services = new ServiceCollection();

            ////// Simple interface registration.
            //services.AddTransient<WorkQueue.Interface.IWorkQueue, WorkQueue.AWS.SQS.SQSWorkQueue>();
            //services.AddTransient<WorkItemStorage.Interface.IWorkItemStore, WorkItemStorage.AWS.S3.S3WorkItemStore>();
            //services.AddTransient<ResultStore.Interface.IResultStore, ResultStore.AWS.S3.S3ResultStore>();
            //services.AddTransient<BatchProcessingClient.Interface.BatchProcessorClient, BatchProcessingClient.Interface.BatchProcessorClient>();

            //var serviceProvider = services.BuildServiceProvider();

            //var client = serviceProvider.GetService<BatchProcessingClient.Interface.BatchProcessorClient>();

            var client = BatchProcessingClient.Interface.BatchProcessorClient.CreateClient();
            ConcurrentBag<string> guids = new ConcurrentBag<string>();
            Stopwatch sw = Stopwatch.StartNew();
            Parallel.For(0, 10, new ParallelOptions() { MaxDegreeOfParallelism = 3 }, i =>
            {
                var guid = Guid.NewGuid().ToString();
                guids.Add(guid);
                client.ScheduleWork(guid, "CopyMoveForgeryDetection",
                    File.ReadAllBytes(imagePath), new System.Collections.Generic.List<BatchProcessor.Model.ParameterInfo>());
                Console.WriteLine(i);
            });
            sw.Stop();
            Parallel.ForEach(guids, guid =>
            {
                while (!client.FinishedProcessing(guid))
                {
                    Thread.Sleep(1000);
                }

                var res = client.GetResult(guid.ToString());
                foreach (var e in res.ExtraParams)
                {
                    Console.WriteLine(e.Name + "=" + e.Value);
                }

                File.WriteAllBytes(res.Key + ".jpg", res.Data);

                Console.WriteLine("============");

            });
        }
    }
}