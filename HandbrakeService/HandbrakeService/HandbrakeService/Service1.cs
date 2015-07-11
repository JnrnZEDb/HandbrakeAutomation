using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace HandbrakeService
{
    public partial class Service1 : ServiceBase
    {
        protected string CurrentDirctory { get { return AppDomain.CurrentDomain.BaseDirectory; } }

        protected string SourceDirctory { get { return System.Configuration.ConfigurationManager.AppSettings["SourceFolder"].ToString(); } }

        protected string TargetDirctory { get { return System.Configuration.ConfigurationManager.AppSettings["TargetFolder"].ToString(); } }

        protected string ExtensionsToListenFor { get { return System.Configuration.ConfigurationManager.AppSettings["ExtensionsToListenFor"].ToString(); } }

        Stack<string> FilesToConvert;

        Stack<FileSystemWatcher> Watchers;

        public Service1()
        {
            InitializeComponent();

            FilesToConvert = new Stack<string>();

            Watchers = new Stack<FileSystemWatcher>();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            var filters = ExtensionsToListenFor.Split(new string[] { "|", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var filter in filters)
            {
                var fileSystemWatcher = new FileSystemWatcher();

                fileSystemWatcher.Path = SourceDirctory;

                fileSystemWatcher.IncludeSubdirectories = true;

                fileSystemWatcher.Filter = filter;

                fileSystemWatcher.EnableRaisingEvents = true;

                fileSystemWatcher.Created += FileSystemWatcher_Created;

                Watchers.Push(fileSystemWatcher);
            }

            ExecuteHandbrake();
        }


        private bool IsRunning { get; set; }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                FilesToConvert.Push(e.FullPath);
            }
        }


        string fileToConvert = "";

        private void ExecuteHandbrake()
        {
            while (!FilesToConvert.Any() || IsRunning)
            {
                System.Threading.Thread.Sleep(TimeSpan.FromMinutes(5));
            }
            
            if (!IsRunning)
            {
                IsRunning = true;

                var fileToConvert = FilesToConvert.Pop();

                var directory = Path.GetDirectoryName(fileToConvert);//Directory.GetDirectories(fileToConvert);

                var tempTargetDirectory = Path.Combine(directory);

                tempTargetDirectory = tempTargetDirectory.Replace(SourceDirctory.Replace('/', '\\'), TargetDirctory.Replace('/', '\\'));

                if (!Directory.Exists(tempTargetDirectory))
                {
                    Directory.CreateDirectory(tempTargetDirectory);
                }

                var outputPath = Path.Combine(tempTargetDirectory, Path.GetFileName(fileToConvert));

                var outputFile = outputPath;

                while (IsFileLocked(fileToConvert))
                {
                    //Wait for copy to finish
                }

                if (!File.Exists(outputFile))
                {
                    var variedParams = "-q 5 -O -U";//Quality;

                    var argument = string.Format("-i \"{0}\" -o \"{1}\" {2} ", fileToConvert, outputFile, variedParams);

                    var p = Process.Start("HandBrakeCLI.exe", argument);

                    p.WaitForExit();
                }

                IsRunning = false;

                ExecuteHandbrake();

            }
            else
            {
                ExecuteHandbrake();
            }

        }

        protected override void OnStop()
        {
            foreach (var fsw in Watchers)
            {
                fsw.Dispose();
            }
        }

        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;
        private bool IsFileLocked(string file)
        {
            //check that problem is not in destination file
            if (File.Exists(file) == true)
            {
                FileStream stream = null;
                try
                {
                    stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (Exception ex2)
                {
                    //_log.WriteLog(ex2, "Error in checking whether file is locked " + file);
                    int errorCode = Marshal.GetHRForException(ex2) & ((1 << 16) - 1);
                    if ((ex2 is IOException) && (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION))
                    {
                        return true;
                    }
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }
            }
            return false;
        }

    }
}
