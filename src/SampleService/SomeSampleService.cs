using AsyncService;
using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SampleService
{
    partial class SomeSampleService : AsyncServiceBase
    {
        public SomeSampleService()
        {
            InitializeComponent();
            this.IncludeExceptionDetailsInEventLog = true;
        }

        //your task-based service implementation goes here
        public override async Task RunServiceAsync(string[] args, CancellationToken cancellationToken, PauseToken pauseToken)
        {
            //define some task
            var task = SomeLongRunningTask(cancellationToken, pauseToken);

            //await completion
            await task;
        }

        private async Task SomeLongRunningTask(CancellationToken cancellationToken, PauseToken pauseToken)
        {
            int count = 10;
            for (int i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"I'm going to throw an error in {count - i} seconds ...");
                await Task.Delay(1000, cancellationToken);
                await pauseToken.WaitWhilePausedAsync(cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                throw new ApplicationException("Simulating an application error");
            }

        }
    }
}
