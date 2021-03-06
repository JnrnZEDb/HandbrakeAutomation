﻿using System;
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

        protected int WaitInterval { get { return int.Parse(System.Configuration.ConfigurationManager.AppSettings["WaitInterval"].ToString()); } }

        List<string> FilesToConvert;

        List<FileSystemWatcher> Watchers;

        public Service1()
        {
            InitializeComponent();

            FilesToConvert = new List<string>();

            Watchers = new List<FileSystemWatcher>();
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

                Watchers.Add(fileSystemWatcher);
            }

            ExecuteHandbrake();
        }


        private bool IsRunning { get; set; }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                FilesToConvert.Add(e.FullPath);
            }
        }


        string fileToConvert = "";

        private void ExecuteHandbrake()
        {
            while (!FilesToConvert.Any() || IsRunning)
            {
                AddMissingItems();

                System.Threading.Thread.Sleep(TimeSpan.FromMinutes(WaitInterval));
            }

            if (!IsRunning)
            {
                IsRunning = true;

                var fileToConvert = FilesToConvert.First();

                //var outputPath = Path.Combine(tempTargetDirectory, Path.GetFileName(fileToConvert));

                var outputFile = fileToConvert.Replace(SourceDirctory, TargetDirctory);

                var directory = Path.GetDirectoryName(fileToConvert);//Directory.GetDirectories(fileToConvert);

                var tempTargetDirectory = Path.Combine(directory);

                tempTargetDirectory = tempTargetDirectory.Replace(SourceDirctory.Replace('/', '\\'), TargetDirctory.Replace('/', '\\'));

                if (!Directory.Exists(tempTargetDirectory))
                {
                    Directory.CreateDirectory(tempTargetDirectory);
                }



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

                    if (p.ExitCode == 2)
                    {
                        var file = AppDomain.CurrentDomain.BaseDirectory + "\\log.txt";

                        if (!File.Exists(file))
                        {
                            File.Create(file);
                        }

                        using (var sw = File.AppendText(file))
                        {
                            sw.WriteLine("Errored: " + fileToConvert);


                            var copyDestination = fileToConvert.Replace(SourceDirctory, TargetDirctory);

                            File.Copy(fileToConvert, copyDestination);

                            sw.WriteLine("-- Copied to: " + copyDestination);
                        }


                    }
                }

                IsRunning = false;

                FilesToConvert.RemoveAt(0);

                ExecuteHandbrake();

            }
            else
            {
                ExecuteHandbrake();
            }

        }

        private void AddMissingItems()
        {
            var folders = Directory.GetDirectories(SourceDirctory);

            if (folders.Any())
            {
                foreach (var folder in folders)
                {
                    var path = folder.Replace(SourceDirctory, TargetDirctory);

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                        .Where(a => a.EndsWith("avi") || a.EndsWith("flv")
                        || a.EndsWith("mkv") || a.EndsWith("mov") || a.EndsWith("mpg")
                        || a.EndsWith("rm") || a.EndsWith("wmv") || a.EndsWith("vob") || a.EndsWith("mp4")
                        || a.EndsWith("m4v"));

                    if (files.Any())
                    {
                        var message = string.Format("Found {0} file(s) in the '{1}' directory", files.Count(), folder);

                        var outPutPath = System.IO.Path.Combine(path, System.IO.Path.GetFileName(folder));

                        foreach (var file in files)
                        {
                            var targetPath = file.Replace(SourceDirctory, TargetDirctory);

                            var replaceValue = Path.GetFileName(targetPath);

                            var newValue = Path.GetFileNameWithoutExtension(targetPath) + ".mp4";

                            targetPath = targetPath.Replace(replaceValue, newValue);

                            if (!File.Exists(System.IO.Path.Combine(targetPath)))
                            {
                                FilesToConvert.Add(file);
                            }
                        }
                    }
                }

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
                    stream = new FileInfo(file).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    //stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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
