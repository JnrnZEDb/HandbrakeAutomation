using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace HandbrakeService.Services
{
    public class SingleFolderWatcher : BaseCompressor, ICompressor
    {
        public List<string> FilesToConvert { get; set; } = new List<string>();

        public void Compress()
        {
            while (!FilesToConvert.Any())
            {
                CheckDirectories(base.SourceDirctory);

                Thread.Sleep(TimeSpan.FromMinutes(base.WaitInterval));
            }

            var files = FilesToConvert.ToList();

            if (files.Any())
            {
                foreach (var file in files)
                {
                    while (IsFileLocked(file))
                    {
                    }

                    var guid = Guid.NewGuid();

                    var currentName = Path.GetFileName(file);

                    var extension = Path.GetExtension(file);

                    var newFile = file.Replace(currentName, guid.ToString()) + extension;

                    File.Move(file, newFile);

                    while (IsFileLocked(newFile))
                    {
                    }

                    var timer = new Stopwatch();
                    timer.Start();

                    var variedParams = "-q 2 -O -U --subtitle 0 --native-language eng";

                    var argument = string.Format("-i \"{0}\" -o \"{1}\" {2} ", newFile, file, variedParams);

                    var p = Process.Start("HandBrakeCLI.exe", argument);

                    p.WaitForExit();

                    timer.Stop();

                    if (p.ExitCode == 2)
                    {
                        var logFile = AppDomain.CurrentDomain.BaseDirectory + "\\log.txt";

                        if (!File.Exists(logFile))
                        {
                            File.Create(logFile);
                        }

                        using (var sw = File.AppendText(file))
                        {
                            sw.WriteLine("Errored: " + file);
                        }
                    }
                    else
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(newFile, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                        var parentPath = Path.GetFullPath(file).Replace(Path.GetFileName(file), "");

                        string message = string.Format("Successfully converted file at {0}, and took {1} ms", DateTime.Now, timer.ElapsedMilliseconds.ToString() );

                        File.WriteAllText(parentPath + @"\" + Path.GetFileName(file) + ".converted", message);
                    }

                    FilesToConvert.Remove(file);
                }
            }

            Compress();
        }


        public void CheckDirectories(string directory)
        {
            var directories = Directory.GetDirectories(directory);

            if (directories.Any())
            {
                foreach (var dir in directories)
                {
                    CheckDirectories(dir);
                }
            }

            var extensions = ExtensionsToListenFor.Split(',');

            var files = Directory.GetFiles(directory)
                .Where(a => extensions.Contains(Path.GetExtension(a).ToLower()))
                .ToList();

            //if (!files.Any(a => !Path.GetFileName(a).Equals("success.converted")))
            //{
            //    FilesToConvert.AddRange(files);
            //}

            if (!File.Exists(Path.Combine(directory, "success.converted")))
            {
                foreach (var file in files)
                {
                    if (!File.Exists(file + ".converted"))
                    {
                        FilesToConvert.Add(file);
                    }
                }
            }
        }
    }
}
