using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HandbrakeService.Services
{
    public abstract class BaseCompressor
    {
        protected string CurrentDirctory { get { return AppDomain.CurrentDomain.BaseDirectory; } }

        protected string SourceDirctory { get { return System.Configuration.ConfigurationManager.AppSettings["SourceFolder"].ToString(); } }

        protected string TargetDirctory { get { return System.Configuration.ConfigurationManager.AppSettings["TargetFolder"].ToString(); } }

        protected string ExtensionsToListenFor { get { return System.Configuration.ConfigurationManager.AppSettings["ExtensionsToListenFor"].ToString(); } }

        protected double WaitInterval { get { return double.Parse(System.Configuration.ConfigurationManager.AppSettings["WaitInterval"].ToString()); } }

        private int ERROR_SHARING_VIOLATION = 32;

        private int ERROR_LOCK_VIOLATION = 33;

        protected bool IsFileLocked(string file)
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

    public interface ICompressor
    {
        void Compress();

    }
}
