using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using AsyncService.Extensions;

namespace AsyncService
{
    /// <summary>
    /// 
    /// </summary>
    [TypeDescriptionProvider(typeof(AbstractTypeDescriptionProvider<AsyncServiceBase, ServiceBase>))]
    public abstract class AsyncServiceBase : ServiceBase
    {
        private AsyncContextThread thread;
        private CancellationTokenSource cts;
        private PauseTokenSource pts;
        private Task runTask;
        private AsyncServiceStatus currentStatus;

        public event EventHandler<AsyncServiceStatusChangedEventArgs> StatusChanged;
        public event EventHandler<AsyncServiceFaultedEventArgs> Faulted;

        public AsyncServiceStatus Status
        {
            get { return currentStatus; }
            private set
            {
                if (value != currentStatus)
                {
                    var oldStatus = currentStatus;
                    currentStatus = value;
                    OnStatusChanged(new AsyncServiceStatusChangedEventArgs(oldStatus, currentStatus));
                }
            }
        }

        protected AsyncServiceBase()
        {
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.AutoLog = true;
        }


        public abstract Task RunServiceAsync(string[] args,
            CancellationToken cancellationToken = default(CancellationToken), 
            PauseToken pauseToken = default(PauseToken));

        public static void RunInteractive(AsyncServiceBase service)
        {
            string[] args = Environment.GetCommandLineArgs();
            service.Run(args);
        }

        protected override void OnStart(string[] args)
        {
            cts = new CancellationTokenSource();
            pts = new PauseTokenSource();
            thread = new AsyncContextThread();

            try
            {
                this.ExitCode = 0;
                this.Status = AsyncServiceStatus.StartPending;
                runTask = thread.Factory.Run(() => RunServiceAsync(args, cts.Token, pts.Token));

                runTask.ContinueWith(t => Stop(), CancellationToken.None,
                    TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                runTask.ContinueWith(t => Debug.WriteLine("ReceiveTask canceled"), CancellationToken.None,
                    TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default);
                runTask.ContinueWith(t => HandleException(t.Exception), CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

                runTask.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
            }
            finally
            {
                this.Status = AsyncServiceStatus.Running;
            }
        }

        private void HandleException(Exception exception)
        {
            if (exception is AggregateException)
            {
                exception = exception.InnerException;
            }

            this.ExitCode = 1064;

            EventLog.WriteEntry(exception?.ToString(), EventLogEntryType.Error);
            OnFaulted(new AsyncServiceFaultedEventArgs(exception));
            
            Stop();
        }

        protected override void OnPause()
        {
#if (DEBUG)
            if (!this.CanPauseAndContinue)
                throw new NotSupportedException("Service is not configured to support PauseAndContinue.");
#endif
            try
            {
                this.Status = AsyncServiceStatus.PausePending;
                pts.IsPaused = true;
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
            }
            finally
            {
                this.Status = AsyncServiceStatus.Paused;
            }
        }

        protected override void OnContinue()
        {
#if (DEBUG)
            if (!this.CanPauseAndContinue)
                throw new NotSupportedException("Service is not configured to support PauseAndContinue.");
#endif
            try
            {
                this.Status = AsyncServiceStatus.ContinuePending;
                pts.IsPaused = false;
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
            }
            finally
            {
                this.Status = AsyncServiceStatus.Running;
            }
        }

        protected override void OnStop()
        {
            try
            {
                this.Status = AsyncServiceStatus.StopPending;

                if (!runTask.IsCompleted)
                {
                    Debug.WriteLine("Cancelling RunTask...");
                    cts.Cancel();
                }

                Debug.WriteLine("Thread.JoinAsync()");
                thread.JoinAsync().GetAwaiter().GetResult();
            }

            catch (Exception e)
            {
                EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
            }
            finally
            {
                this.Status = AsyncServiceStatus.Stopped;
            }
        }


        protected void OnStatusChanged(AsyncServiceStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        protected void OnFaulted(AsyncServiceFaultedEventArgs e)
        {
            Faulted?.Invoke(this, e);
        }
    }
}