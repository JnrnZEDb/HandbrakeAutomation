using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace HandbrakeService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
//#if DEBUG
            new Service1()
                .OnDebug();

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
//#endif
//#if !(DEBUG)

//            ServiceBase[] ServicesToRun;
//            ServicesToRun = new ServiceBase[]
//            {
//                new Service1()
//            };
//            ServiceBase.Run(ServicesToRun);
//#endif
        }
    }
}
