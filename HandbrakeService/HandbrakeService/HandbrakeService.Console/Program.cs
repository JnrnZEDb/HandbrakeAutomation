using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HandbrakeService.Console
{
    class Program
    {
        static void Main(string[] args)
        {

            OnStart(null);
        }


        protected static void OnStart(string[] args)
        {
            var compression = new HandbrakeService.Services.SingleFolderWatcher();

            compression.Compress();

            return;
        }

    }
}
