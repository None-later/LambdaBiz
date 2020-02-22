# LambdaBiz

**Long running stateful orchestrations in C# using AWS Lambda.**
AWS lambda enables users to write serverless functions. However, a lambda function can have a maximum execution time of 15 minutes after which it times out. Hence, it is not possible to write long running processes in AWS lambda. AWS has introduced step functions to overcome this shortcomming. However, there is a steep learning curve to learn the state machine language and the service itself comes at a premium cost.

The purpose of this project is to enable existing C# users of AWS to write long running orchestrations which are durable. 

## Durability
The framework relies upon the AWS SWF (Simple Workflow Framework) to maintain the durability of the orchestration. If a sequence of tasks has been executed and the sequence times out and is called again, the framework will not call the already executed tasks in the sequence and the orchestration will continue from the point where it was left during the last run.

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

## Get Orchestartion status from the persistent store
The framework can be queried to get the current status of the orchestration. The state of the orchestration is saved periodically in the persistent store which is DynamoDB for AWS. **This will only be active if the corresponding paramter in the construction of AWSORchestrtaionFactory is set to TRUE**.
```C#
/// Wait for user input
var currentState = await orchestration.GetCurrentState();
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

