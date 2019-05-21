using AsyncService.Extensions;
using Nito.AsyncEx;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncService
{
    /// <summary>
    /// An abstract implementation of <see cref="System.ServiceProcess.ServiceBase"/> aimed at running a Task-based Windows Service.
    /// </summary>
    [TypeDescriptionProvider(typeof(AbstractTypeDescriptionProvider<AsyncServiceBase, ServiceBase>))]
    public abstract class AsyncServiceBase : ServiceBase
    {
        private AsyncContextThread _contextThread; 
        private CancellationTokenSource _cts; 
        private PauseTokenSource _pts;
        private Task _runTask;
        private AsyncServiceStatus _currentStatus;

        /// <summary>
        /// Occurs when the internal ServiceStatus changes.
        /// </summary>
        /// <remarks>Should only be used for testing purposes.</remarks>
        public event EventHandler<AsyncServiceStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Occurs when the main execution task completes with an exception.
        /// </summary>
        /// <remarks>Should only be used for testing purposes.</remarks>
        public event EventHandler<AsyncServiceFaultedEventArgs> Faulted;

        /// <summary>
        /// Gets/sets a value indicating if exception details including StackTrace should be logged to the Windows Application EventLog.
        /// </summary>
        public bool IncludeExceptionDetailsInEventLog { get; set; }

        /// <summary>
        /// The internal status of the service.
        /// </summary>
        /// <remarks>Should only be used for testing purposes.</remarks>
        public AsyncServiceStatus Status
        {
            get { return _currentStatus; }
            private set
            {
                if (value != _currentStatus)
                {
                    var oldStatus = _currentStatus;
                    _currentStatus = value;
                    OnStatusChanged(new AsyncServiceStatusChangedEventArgs(oldStatus, _currentStatus));
                }
            }
        }

        /// <summary>
        /// Initializes the <see cref="AsyncServiceBase"/>.
        /// Should be overriden in derived class to set default properties.
        /// </summary>
        protected AsyncServiceBase()
        {
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.AutoLog = true;
            this.ServiceName = nameof(AsyncServiceBase);
        }

        /// <summary>
        /// The execution task of the service. User-specific implementation goes here.
        /// Appropriately sets ServiceBase.ExitCode depending on Task completion state and logs exceptions to Windows Application Eventlog.
        /// </summary>
        /// <param name="args">The commandline arguments passed to the service.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to signal stopping of the service.</param>
        /// <param name="pauseToken">The <see cref="PauseToken"/> used to signal pausing of the service.</param>
        /// <returns>A <see cref="Task"/> task.</returns>
        public abstract Task RunServiceAsync(string[] args, CancellationToken cancellationToken, PauseToken pauseToken);

        /// <summary>
        /// Runs the given <see cref="AsyncServiceBase"/> service interactively.
        /// Used for debugging purposes only.
        /// </summary>
        /// <param name="service">The <see cref="AsyncServiceBase"/> service to run.</param>
        public static void RunInteractive(AsyncServiceBase service)
        {
            string[] args = Environment.GetCommandLineArgs();
            service.Run(args);
        }

        /// <summary>
        /// <see cref="ServiceBase.OnStart"/> override.
        /// Creates an internal <see cref="Task"/> which is run on the <see cref="AsyncContextThread"/> and awaits its completion without blocking.
        /// Appropriately sets ServiceBase.ExitCode depending on Task completion state and logs exceptions to Windows Application Eventlog.
        /// </summary>
        /// <param name="args">The commandline arguments passed to the service.</param>
        protected override void OnStart(string[] args)
        {
            _cts = new CancellationTokenSource();
            _pts = new PauseTokenSource();
            _contextThread = new AsyncContextThread();

            try
            {
                this.ExitCode = 0;
                this.Status = AsyncServiceStatus.StartPending;

                // create a new long running task
                _runTask = _contextThread.Factory.Run(() => RunServiceAsync(args, _cts.Token, _pts.Token));

                // if the task completed normally, just stop the service and exit
                _runTask.ContinueWith(t => Stop(), CancellationToken.None,
                    TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                
                // if the task was canceled in OnStop() just write a debug message
                _runTask.ContinueWith(t => Debug.WriteLine("ReceiveTask canceled"), CancellationToken.None,
                    TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default);
                
                // if the task faulted, handle the exception
                _runTask.ContinueWith(t => HandleException(t.Exception), CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

                // await the completion of the task without blocking the OnStart() call from SCM
                _runTask.WaitAsync(_cts.Token).ConfigureAwait(false);
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

        /// <summary>
        /// Logs the Task Exception to the Application Eventlog, sets the ExitCode and stops the service.
        /// </summary>
        /// <param name="exception"></param>
        private void HandleException(Exception exception)
        {
            if (exception is AggregateException)
                exception = exception.InnerException;
            

            // set the Exitcode so that SCM can perform recovery
            this.ExitCode = 1066; // The service has returned a service-specific error code.

            if (exception != null)
            {
                // log the Task exception, details depending on IncludeExceptionDetailsInEventLog
                var message = IncludeExceptionDetailsInEventLog ? exception.ToString() : exception.Message;
                EventLog.WriteEntry(message, EventLogEntryType.Error);

                OnFaulted(new AsyncServiceFaultedEventArgs(exception));
            }

            //Stop the service
            Stop();
        }

        /// <summary>
        /// <see cref="ServiceBase.OnPause"/> override.
        /// Sets the internal <see cref="PauseTokenSource"/> to true to signal pausing of the service.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        protected override void OnPause()
        {
#if (DEBUG)
            // only valid during DEBUG as SCM will not call OnPause if PauseAndContinue is not set.
            if (!this.CanPauseAndContinue)
                throw new NotSupportedException("Service is not configured to support PauseAndContinue.");
#endif
            try
            {
                this.Status = AsyncServiceStatus.PausePending;
                _pts.IsPaused = true;
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

        /// <summary>
        /// <see cref="ServiceBase.OnContinue"/> override.
        /// Sets the internal <see cref="PauseTokenSource"/> to false to signal continuation of the service.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        protected override void OnContinue()
        {
#if (DEBUG)
            // only valid during DEBUG as SCM will not call OnContinue if PauseAndContinue is not set.
            if (!this.CanPauseAndContinue)
                throw new NotSupportedException("Service is not configured to support PauseAndContinue.");
#endif
            try
            {
                this.Status = AsyncServiceStatus.ContinuePending;
                _pts.IsPaused = false;
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

        /// <summary>
        /// <see cref="ServiceBase.OnStop"/> override.
        /// Cancels the internal <see cref="CancellationTokenSource"/> to signal stopping of the service if the service has not already been stopped.
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                this.Status = AsyncServiceStatus.StopPending;

                // we only need to cancel the task if it hasn't already been cancelled
                if (!_runTask.IsCompleted)
                {
                    Debug.WriteLine("Cancelling RunTask...");
                    _cts.Cancel();
                }

                // await the end of all asynchronous tasks
                Debug.WriteLine("Thread.JoinAsync()");
                _contextThread.JoinAsync().GetAwaiter().GetResult();
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

        /// <summary>
        /// Invokes StatusChanged event
        /// </summary>
        /// <remarks>Deliberately not async as used only for interactive testing</remarks>        
        /// <param name="e"></param>
        protected void OnStatusChanged(AsyncServiceStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Invokes Faulted event
        /// </summary>
        /// <remarks>Deliberately not async as used only for interactive testing</remarks>        
        /// <param name="e"></param>
        protected void OnFaulted(AsyncServiceFaultedEventArgs e)
        {
            Faulted?.Invoke(this, e);
        }
    }
}