# AsyncServiceBase
An abstract implementation of [System.ServiceProcess.ServiceBase](https://docs.microsoft.com/en-us/dotnet/api/system.serviceprocess.servicebase) aimed at implementing Task-based Windows Services, including interactive testing facilities.

Supports `.NET Framework 4.7.2`

## Getting started

`AsyncServiceBase` is an abstraction layer around System.ServiceProcess.ServiceBase wich provides a single abstract method **RunServiceAsync()** to run any Task-related work.

```C#
//your task-based service implementation goes here
public override async Task RunServiceAsync(string[] args, CancellationToken cancellationToken, PauseToken pauseToken)
{
    //define some task
    var task = SomeLongRunningTask(cancellationToken, pauseToken);

    //await completion
    await task;
}
```

Calls from Service Control Manager (SCM) to **OnStart**, **OnStop**, **OnPause** and **OnContinue** are handled internally by `AsyncServiceBase` to appropriately control starting, stopping and pausing of the Task via the [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken) and [PauseToken](https://github.com/StephenCleary/AsyncEx). 

It's important to await the completion of the internal task.

Upon task-completion, `AsyncServiceBase` will set the service's ExitCode accordingly based on the completion status of the task and will log any exception to the Windows Application eventlog.

## Interactive debugging
`AsyncServiceBase` provides a static **RunInteractive** method which can be used to run and debug your specific service implementation interactively within a console application. Depending on the state-changes your service supports, you can start, stop, pause and continue your service. 

Usage:
```C#
static void Main(string[] args)
{
    var service = new SomeSampleService();
    if (Environment.UserInteractive)
    {
        AsyncServiceBase.RunInteractive(service);
    }
    else
    {
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[] { service };
        ServiceBase.Run(ServicesToRun);
    }
}
```
Please see the included SampleService application for a sample implementation.


#
### Acknowledgements

A very special thanks goes to Stephan Cleary for his Nito.AsyncEx library, which is heavily used in AsyncServiceBase.
