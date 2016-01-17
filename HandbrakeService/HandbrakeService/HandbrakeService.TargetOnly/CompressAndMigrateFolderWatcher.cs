using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandbrakeService.Services
{
    public class CompressAndMigrateFolderWatcher : BaseCompressor, ICompressor
    {
        List<string> FilesToConvert = new List<string>();

        List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();

        public void Compress()
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
                
                var outputFile = fileToConvert.Replace(SourceDirctory, TargetDirctory);

                var directory = Path.GetDirectoryName(fileToConvert);

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
                    var variedParams = "-q 5 -O -U --subtitle 0 --native-language eng";//Quality;

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

                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(fileToConvert, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }
                else
                {

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
                        .Where(a => base.ExtensionsToListenFor.Split(new char[] { ',', '|' }).Contains(Path.GetExtension(a)));                  

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

                            if (!File.Exists(System.IO.Path.Combine(targetPath))
                                && !File.Exists(targetPath.Replace(newValue, replaceValue)))
                            {
                                FilesToConvert.Add(file);
                            }
                        }
                    }
                }

            }
        }
    }
}
