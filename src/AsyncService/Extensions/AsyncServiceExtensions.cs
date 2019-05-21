using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace AsyncService.Extensions
{
    internal static class AsyncServiceExtensions
    {
        /// <summary>
        /// Runs the given <see cref="AsyncServiceBase"/> service interactively
        /// </summary>
        /// <param name="service">The <see cref="AsyncServiceBase"/> service to run.</param>
        /// <param name="args">Commandline arguments passed to the service.</param>
        internal static void Run(this AsyncServiceBase service, string[] args)
        {
            if (!Environment.UserInteractive)
                throw new NotSupportedException("Not running in UserInteractive mode.");
            
            var thread = new AsyncContextThread();
            var task = thread.Factory.Run(() => RunAsync(service, args));
            task.Wait();
            thread.Join();
        }

        /// <summary>
        /// Execution task for the service
        /// </summary>
        /// <param name="service">The <see cref="AsyncServiceBase"/> service to run.</param>
        /// <param name="args">Commandline arguments passed to the service.</param>
        private static async Task RunAsync(AsyncServiceBase service, string[] args)
        {
            Console.WriteLine("Running interactive");

            service.Faulted += Service_Faulted;
            service.StatusChanged += Service_StatusChanged;
            service.Start(args);

            while (true)
            {
                try
                {
                     var result = await ReadKeyAsync(CancellationToken.None);
                     HandleKeyPressed(result, service, args);
                }
                catch (OperationCanceledException)
                {
                    if (service.Status != AsyncServiceStatus.Stopped)
                    {
                        service.Stop();
                    }
                    break;
                }
                catch (TargetInvocationException e)
                {
                    Console.WriteLine(e.InnerException?.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Service ended with ExitCode {service.ExitCode}");
            Console.WriteLine("Press any key to quit");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Handles Service faults by displaying the exception
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Service_Faulted(object sender, AsyncServiceFaultedEventArgs e)
        {
            var service = (AsyncServiceBase)sender;
            Console.WriteLine($"Faulted: {e.Fault.Message} ExitCode: {service.ExitCode}");
        }

        /// <summary>
        /// Handle KeyPressed
        /// </summary>
        /// <param name="keyInfo"></param>
        /// <param name="service"></param>
        /// <param name="args"></param>
        private static void HandleKeyPressed(ConsoleKeyInfo keyInfo, AsyncServiceBase service, string[] args)
        {
            if (keyInfo.Key == ConsoleKey.P & service.Status == AsyncServiceStatus.Running)
            {
                service.Pause();
            }
            else if (keyInfo.Key == ConsoleKey.R & service.Status == AsyncServiceStatus.Stopped)
            {
                service.Start(args);
            }
            else if (keyInfo.Key == ConsoleKey.C & service.Status == AsyncServiceStatus.Paused)
            {
                service.Continue();
            }
            else if (keyInfo.Key == ConsoleKey.S & service.Status == AsyncServiceStatus.Running)
            {
                service.Stop();
            }
        }

        /// <summary>
        /// Handle ServiceStatus changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Service_StatusChanged(object sender, AsyncServiceStatusChangedEventArgs e)
        {
            var instance = (AsyncServiceBase)sender;

            Console.WriteLine($"Status: {instance.Status}");

            Console.ForegroundColor = ConsoleColor.White;
            if (instance.Status == AsyncServiceStatus.Running & instance.CanPauseAndContinue)
            {
                Console.WriteLine("Press 'P' to pause, 'S' to stop or 'Ctrl-C' to quit...");
            }
            else if (instance.Status == AsyncServiceStatus.Running & instance.CanStop)
            {
                Console.WriteLine("Press 'S' to stop or 'Ctrl-C' to quit...");
            }
            else if (instance.Status == AsyncServiceStatus.Paused)
            {
                Console.WriteLine("Press 'C' to continue or 'Ctrl-C' to quit...");
            }
            else if (instance.Status == AsyncServiceStatus.Stopped)
            {
                Console.WriteLine("Press 'R' to run or 'Ctrl-C' to quit...");
            }
            Console.ResetColor();
        }

        
        /// <summary>
        /// Helper method to access ServiceBase protected member
        /// </summary>
        /// <param name="serviceInstance"></param>
        /// <param name="args"></param>
        private static void Start(this ServiceBase serviceInstance, string[] args)
        { 
            serviceInstance.GetType().
                InvokeMember("OnStart", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, serviceInstance, new object[] { args });
        }

        /// <summary>
        /// Helper method to access ServiceBase protected member
        /// </summary>
        /// <param name="serviceInstance"></param>
        private static void Pause(this ServiceBase serviceInstance)
        {
            serviceInstance.GetType().
                InvokeMember("OnPause", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, serviceInstance, new object[] {});
        }

        /// <summary>
        /// Helper method to access ServiceBase protected member
        /// </summary>
        /// <param name="serviceInstance"></param>
        private static void Continue(this ServiceBase serviceInstance)
        {
            serviceInstance.GetType().
                InvokeMember("OnContinue", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, serviceInstance, new object[] { });
        }

        /// <summary>
        /// Non-blocking Console.ReadKey
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="responsiveness"></param>
        /// <returns></returns>
        private static async Task<ConsoleKeyInfo> ReadKeyAsync(CancellationToken cancellationToken, int responsiveness = 100)
        {
            try
            {
                Console.TreatControlCAsInput = true;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var cki = Console.ReadKey(true);
                        if (((cki.Modifiers & ConsoleModifiers.Control) != 0)  & cki.Key == ConsoleKey.C)
                        {
                            break;
                        }
                        return cki;
                    }
                    await Task.Delay(responsiveness, cancellationToken);
                }

                throw new TaskCanceledException();
            }
            finally
            {
                Console.TreatControlCAsInput = false;
            }
        }
    }
}