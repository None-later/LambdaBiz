# LambdaBiz

**Long running stateful orchestrations in C# using AWS Lambda.**
AWS lambda enables users to write serverless functions. However, a lambda function can have a maximum execution time of 15 minutes after which it times out. Hence, it is not possible to write long running processes in AWS lambda. AWS has introduced step functions to overcome this shortcomming. However, there is a steep learning curve to learn the state machine language and the service itself comes at a premium cost.

The purpose of this project is to enable existing C# users of AWS to write long running orchestrations which are durable. 

## Durability
The framework relies upon the AWS SWF (Simple Workflow Framework) to maintain the durability of the orchestration. If a sequence of tasks has been executed and the sequence times out and is called again, the framework will not call the already executed tasks in the sequence and the orchestration will continue from the point where it was left during the last run.

## Terminology

**OrchestrationFactory** A store in AWS for creating orchestrations and saving them in a persistent store (if the parameter is set). The persistent storage is AWS DynamoDB. AWS SWF stores the state of orchestrations for 1 year.

**Orchestration** A Workflow instance which is identified by a unique **OrchestrationId**.

**Task** An AWS lambda function called in an orchestration identified by a unique **OperationId**.

**Timer** A timer identified by a unique **TimerId**.

**Event** An external trigger identified by a unique **EventId**.

**Service** A call to an external REST Service (GET,POST,PUT or DELETE) identified by a unique **OperationId**.

## Orchestrating lambda tasks
```C#
/// Initialize orchestration factory for AWS
var orchestrationFactory = new AWSOrchestrationFactory(awsAccessKeyID, awsSecretAccessKey, awsRegion, true,awsLambdaRole);

/// Create a new orchestration
var orchestration = await orchestrationFactory.CreateOrchestrationAsync("Sequence3");
try
{
            /// Start workflow
            await orchestration.StartWorkflowAsync("Workflow Started");

            /// Call AWS lambda task
            var a = await orchestration.CallTaskAsync<Numbers>("Number", 
                        new Numbers { 
                                    Number1 = 15, 
                                    Number2 = 5 
                                    }, 
                        "Operation1");

            var b = await orchestration.CallTaskAsync<OperationResult>("Sum", a, "Operation2");
            var c = await orchestration.CallTaskAsync<OperationResult>("Difference", a, "Operation3");
            var d = await orchestration.CallTaskAsync<OperationResult>("Product", a, "Operation4");
            var e = await orchestration.CallTaskAsync<OperationResult>("Quotient", a, "Operation5");

            /// Complete workflow
            await orchestration.CompleteWorkflowAsync(e);
}
catch(Exception ex)
{
            /// Fail workflow
            await orchestration.FailWorkflowAsync(ex);
}
```

## Timers
```C#
/// Start timer
await orchestration.StartTimerAsync("30SecTimer", new TimeSpan(0, 0, 0, 30, 0));
```

## Wait for user input
```C#
/// Wait for user input
var approved = await orchestration.WaitForEventAsync<bool>("Approve");
```
## Raise event in another orchestration
```C#
/// Wait for user input
await orchestration.RaiseEventAsync("Approve", "Sequence3", true);
```


## Get Orchestartion status from the persistent store
The framework can be queried to get the current status of the orchestration. The state of the orchestration is saved periodically in the persistent store which is DynamoDB for AWS. **This will only be active if the corresponding paramter in the construction of AWSORchestrtaionFactory is set to TRUE**.
```C#
/// Wait for user input
var currentState = await orchestration.GetCurrentState();
```
## Put it all together
Create a Serverless project using the AWS toolkit for Visual Studio.

### Serverless template
```C#
{
	"AWSTemplateFormatVersion" : "2010-09-09",
	"Transform" : "AWS::Serverless-2016-10-31",
	"Description" : "An AWS Serverless Application.",
	"Resources" : {

		"Process" : {
			"Type" : "AWS::Serverless::Function",
			"Properties": {
				"Handler": "LambdaBiz.Serverless::LambdaBiz.Serverless.Functions::Process",
				"Runtime": "dotnetcore2.1",
				"CodeUri": "",
				"MemorySize": 256,
				"Timeout": 30,
				"Role": null,
				"Policies": [ "AWSLambdaBasicExecutionRole" ]
				
			}
		}

	}
}
```

### Create the long running lambda function
```C#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LambdaBiz.AWS;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaBiz.Serverless
{
    public class Functions
    {
        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
        }
        public class Numbers
        {
            public int Number1 { get; set; }
            public int Number2 { get; set; }
        }

        public class Request
        {
            public string LambdaRole { get; set; }
            public Numbers Numbers { get; set; }
            public string OrchestrationId { get; set; }
        }
        public class OperationResult
        {
            public int Number1 { get; set; }
            public int Number2 { get; set; }
            public double Result { get; set; }
        }

        public async Task<Model.Workflow> ProcessAsync(Request request, ILambdaContext context)
        {
            /// Initialize orchestration factory for AWS
            var orchestrationFactory = new AWSOrchestrationFactory(true, request.LambdaRole);

            context.Logger.LogLine(JsonConvert.SerializeObject(request));
            /// Create a new .
            var orchestration = await orchestrationFactory.CreateOrchestrationAsync(request.OrchestrationId);
            context.Logger.LogLine("Created");
            try
            {

                /// Start workflow
                await orchestration.StartWorkflowAsync("Workflow Started");
                context.Logger.LogLine("Started");

                /// Call AWS lambda task
                var a = await orchestration.CallTaskAsync<Numbers>("Number", request.Numbers, "Operation1");
                context.Logger.LogLine("Operation1");

                var b = await orchestration.CallTaskAsync<OperationResult>("Sum", a, "Operation2");
                context.Logger.LogLine("Operation2");

                var c = await  orchestration.CallTaskAsync<OperationResult>("Difference", a, "Operation3");
                context.Logger.LogLine("Operation3");

                var d = await orchestration.CallTaskAsync<OperationResult>("Product", a, "Operation4");
                context.Logger.LogLine("Operation4");

                var e = await  orchestration.CallTaskAsync<OperationResult>("Quotient", a, "Operation5");
                context.Logger.LogLine("Operation5");
                /// Start timer
                await orchestration.StartTimerAsync("30SecTimer", new TimeSpan(0, 0, 0, 30, 0));
                context.Logger.LogLine("30SecTimer");

                /// Wait for user input
                var approved = await orchestration.WaitForEventAsync<bool>("Approve");
                context.Logger.LogLine("Approved");

                /// Complete workflow
                await orchestration.CompleteWorkflowAsync(e);
                context.Logger.LogLine("Complete");
            }
            catch (Exception ex)
            {
                /// Fail workflow
                await orchestration.FailWorkflowAsync(ex);
                context.Logger.LogLine("Fail");
                context.Logger.LogLine(ex.Message);
                context.Logger.LogLine(ex.StackTrace);
            }

            var currentState = await orchestration.GetCurrentState();
            return currentState;
        }

        /// <summary>
        /// Lambda function
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Model.Workflow Process(Request request, ILambdaContext context)
        {
            var result = ProcessAsync(request, context);
            result.Wait();
            return result.Result;
        }
    }
}

```


## Calling external services
Any external service which is exposed as a REST service can be called. Hence, we can also call Azure functions from AWS lamda.
```C#
/// Call REST service
var a = await orchestration.CallGetAsync<DummyResponse>(url + "employees",null,null, "ServiceOperation1");
var b = await orchestration.CallPostAsync<DummyResponse>(url + "create",null, null, null, "ServiceOperation2");
var c = await orchestration.CallPutAsync<DummyResponse>(url + "update/21", null, null, null, "ServiceOperation3");
var d = await orchestration.CallDeleteAsync<DummyResponse>(url + "delete/21", null, null, "ServiceOperation4");
```
**Create an AWS background process to run external services**
```C#
while (true)
{
            var orch = new AWSRESTService(awsAccessKeyID, awsSecretAccessKey, awsRegion);
            await orch.Run("RESTSequence1");
}
```

## Use your own backend
LambdaBiz uses AWS SWF as the default back-end and DynamoDB as the default persistent store. However, it is possible to create your own back-end and store using the framework. Maybe, you want to use SQL,MySql or some other no SQL as the back-end.

### Create your own back-end
Implement **IOrchestrationFactory** to create your own factory based on your back-end.
Implement **IOrchestration** to create your own orchestration container based on your back-end.

### Create your own persistent store
Implement **IPersistantStore** to create your own persistent store.


