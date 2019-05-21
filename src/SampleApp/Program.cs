using AsyncService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
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
