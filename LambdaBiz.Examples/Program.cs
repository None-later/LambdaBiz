﻿using LambdaBiz.AWS;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LambdaBiz.Examples
{
    class Program
    {
        internal class AWS        {
            public string Region { get; set; }
            public string AccessKeyID { get; set; }
            public string SecretAccessKey { get; set; }
            public string LambdaRole { get; set; }
        }

        internal class Numbers
        {
            public int Number1 { get; set; }
            public int Number2 { get; set; }
        }
        internal class OperationResult
        {
            public int Number1 { get; set; }
            public int Number2 { get; set; }
            public double Result { get; set; }
        }
        static void Main(string[] args)
        {
            var contents = File.ReadAllText("D://Work//AWSWorkflowProvider.json");

            var aws = JsonConvert.DeserializeObject<AWS>(contents);
            try
            {

                Sequence(aws).Wait();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        static async Task Sequence(AWS aws)
        {
            var orchestrationFactory = new AWSOrchestrationFactory(aws.AccessKeyID, aws.SecretAccessKey, aws.Region, true,aws.LambdaRole);

            var orchestration = await orchestrationFactory.CreateOrchestrationAsync("Sequence2");

            try {

                await orchestration.StartWorkflowAsync("Workflow Started");

                var a = await orchestration.CallTaskAsync<Numbers>("Number", new Numbers { Number1 = 15, Number2 = 5 }, "Operation1");

                var b = await orchestration.CallTaskAsync<OperationResult>("Sum", a, "Operation2");

                var c = await orchestration.CallTaskAsync<OperationResult>("Difference", a, "Operation3");

                var d = await orchestration.CallTaskAsync<OperationResult>("Product", a, "Operation4");

                var e = await orchestration.CallTaskAsync<OperationResult>("Quotient", a, "Operation5");

                await orchestration.StartTimerAsync("30SecTimer", new TimeSpan(0, 0, 0, 30, 0));

                var approved = await orchestration.WaitForEventAsync<bool>("Approve");

                await orchestration.CompleteWorkflowAsync(e);
            }
            catch(Exception ex)
            {
                await orchestration.FailWorkflowAsync(ex);            }

            var currentState = await orchestration.GetCurrentState();
            
        }
    }
}