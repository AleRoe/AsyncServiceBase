# AsyncServiceBase
An abstract implementation of System.ServiceProcess.ServiceBase aimed at running a Task-based Windows Service.

Supports `.NET Framework 4.7.2`

## Getting started

`AsyncServiceBase` is an abstraction layer around System.ServiceProcess.ServiceBase wich provides a single abstract method RunServiceAsync() to run any Task-related work.

```C#
public override async Task RunServiceAsync(string[] args, CancellationToken cancellationToken = default(CancellationToken), PauseToken pauseToken = default(PauseToken))
        {
            //define some task
            var task = SomeLongRunningTask(cancellationToken, pauseToken);
            // WaitAsync for completion without blocking ServiceBase.OnStart 
            await task.WaitAsync(cancellationToken);
        }
```

Calls from Service Control Manager (SCM) to **OnStart**, **OnStop**, **OnPause** and **OnContinue** are handled internally by `AsyncServiceBase` to appropriately control starting, stopping and pausing of the Task via the [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken) and [PauseToken](https://github.com/StephenCleary/AsyncEx). 

It's important to asynchronously await the completion of the internal task.

Upon task-completion, `AsyncServiceBase` will set the service's ExitCode accordingly based on the completion status of the task and will log any exception to the Windows Application eventlog.

## Testing
`AsyncServiceBase` provides a static **RunInteractive** method similar to the ServiceBase.Run method which can be used to test and debug your specific service implementation interactively within a console application. Depending on the state-changes your service supports, you can start, stop, pause and continue your service. 

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


#
### Acknowledgements

A very special thanks goes to Stephan Cleary for his Nito.AsyncEx library, which is heavily used in AsyncServiceBase.
