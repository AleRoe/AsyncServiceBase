using AsyncService;
using System;
using System.ServiceProcess;

namespace SampleService
{
    static class Program
    {
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
    }
}
