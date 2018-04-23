using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace nddi
{
    public class Run : MarshalByRefObject
    {
        public int Start { get; set; }
        public int Stop { get; set; }
    }

    public class ConfigurationNode : MarshalByRefObject
    {
        private XmlNode Node;

        public ConfigurationNode Parent;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<DayOfWeek, List<Run>> schedule;
        public Run[] GetSchedule(DayOfWeek day)
        {
            if (!schedule.ContainsKey(day))
            {
                if (Parent != null)
                {
                    return Parent.GetSchedule(day);
                }else
                {
                    return new Run[] { };
                }
            }
            return schedule[day].ToArray();
        }

        public void SetSchedule(DayOfWeek day, Run[] run)
        {
            if (!schedule.ContainsKey(day))
            {
                schedule[day] = new List<Run>();
            }
            schedule[day].Clear();
            schedule[day].AddRange(run);
        }

        /// <summary>
        /// 
        /// </summary>
        private string argument;
        public string Argument
        {
            get
            {
                if (string.IsNullOrEmpty(argument))
                {
                    if (Parent != null)
                    {
                        return Parent.Argument;
                    }
                }
                return argument;
            }
            set
            {
                argument = value;
            }
        }

        private Dictionary<string, string> processName;
        public string GetProcessName(string index)
        {
            if (processName == null || !processName.ContainsKey(index))
            {
                if (Parent != null)
                {
                    return Parent.GetProcessName(index);
                }
            }
            return processName[index];
        }

        public void SetProcessName(string key, string value)
        {
            if (processName == null)
            {
                processName = new Dictionary<string, string>();
            }
            processName[key] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        private TimeSpan updateConfigurationTimeSpan;
        public TimeSpan UpdateConfigurationTimeSpan
        {
            get
            {
                if (updateConfigurationTimeSpan.Ticks == 0)
                {
                    if (Parent != null)
                    {
                        return Parent.UpdateConfigurationTimeSpan;
                    }
                }
                return updateConfigurationTimeSpan;

            }
            set
            {
                updateConfigurationTimeSpan = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, string> downloadUrl;
        public string GetDownloadUrl(string index)
        {
            if (downloadUrl == null || !downloadUrl.ContainsKey(index))
            {
                if (Parent != null)
                {
                    return Parent.GetDownloadUrl(index);
                }
            }
            return downloadUrl[index];
        }

        public void SetDownloadUrl(string key, string value)
        {
            if (downloadUrl == null)
            {
                downloadUrl = new Dictionary<string, string>();
            }
            downloadUrl[key] = value;
        }

        public ConfigurationNode()
        {
            downloadUrl = new Dictionary<string, string>();
            processName = new Dictionary<string, string>();
            schedule = new Dictionary<DayOfWeek, List<Run>>();
            argument = null;
        }

        internal void Configure(XmlNode node)
        {
            downloadUrl.Clear();
            schedule.Clear();
            processName.Clear();
            argument = null;
            updateConfigurationTimeSpan = new TimeSpan();
            
            Node = node;
            if (Node != null)
            {
                foreach (XmlNode urlNode in Node.SelectNodes("url"))
                {
                    downloadUrl[urlNode.Attributes["name"].Value] = urlNode.InnerText;
                }

                foreach (XmlNode urlNode in Node.SelectNodes("process"))
                {
                    processName[urlNode.Attributes["name"].Value] = urlNode.InnerText;
                }

                XmlNode argsNode = Node.SelectSingleNode("args");
                if (argsNode != null)
                {
                    argument = argsNode.InnerText;
                }

                XmlNode scheduleNode = node.SelectSingleNode("schedule");
                if (scheduleNode != null)
                {
                    foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        XmlNode dayNode = scheduleNode.SelectSingleNode(string.Format("day[@name='{0}']", day.ToString()));
                        if (dayNode != null)
                        {
                            schedule[day] = new List<Run>();
                            foreach (XmlNode runNode in dayNode.SelectNodes("run"))
                            {
                                int start = -1;
                                int stop = -1;
                                if (
                                    Int32.TryParse(runNode.Attributes["start"].InnerText, out start) &&
                                    Int32.TryParse(runNode.Attributes["stop"].InnerText, out stop)
                                    )
                                {
                                    schedule[day].Add(new Run
                                    {
                                        Start = start,
                                        Stop = stop
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class Configuration : MarshalByRefObject, IDisposable
    {
        private WebClient Client;
        private string ConfigUrl;
        private BackgroundWorker bgConfigure;
        private object lockConfigure = new object();

        public ConfigurationNode DefaultConfigNode
        {
            get
            {
                ConfigurationNode node = configNode;
                while(node.Parent != null)
                {
                    node = node.Parent;
                }
                return node;
            }
        }

        private ConfigurationNode configNode;
        public ConfigurationNode ConfigNode
        {
            get
            {
                return configNode;
            }
            private set
            {
                configNode = value;
            }
        }
        public string TempDirectory;
        public string WorkingDirectory;
        public string Hostname;

        public Configuration(string configUrl)
        {
            Hostname = GetHostName();

            ConfigUrl = configUrl;
            Client = new WebClient();
            Client.Proxy = null;
            Client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            Client.Headers.Add("Cache-Control", "no-cache");

            // prepare temp directory
            TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            WorkingDirectory = Path.Combine(TempDirectory, "working");
            Directory.CreateDirectory(TempDirectory);
            Directory.CreateDirectory(WorkingDirectory);

            configNode = new ConfigurationNode();
            configNode.Parent = new ConfigurationNode();
            configNode.Parent.Parent = new ConfigurationNode();

            bgConfigure = new BackgroundWorker();
            bgConfigure.DoWork += ReConfigure;
            bgConfigure.WorkerSupportsCancellation = true;
            bgConfigure.RunWorkerAsync();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool b)
        {
            bgConfigure.CancelAsync();
        }

        private void ReConfigure(object o, DoWorkEventArgs e)
        {
            DateTime latRunTime = new DateTime();
            while (!bgConfigure.CancellationPending)
            {
                if ((DateTime.Now - latRunTime) > ConfigNode.UpdateConfigurationTimeSpan)
                {
                    Configure();
                    latRunTime = DateTime.Now;
                }
                Thread.Sleep(100);
            }
        }

        internal void Configure()
        {
            lock (lockConfigure)
            {
                try
                {
                    string xmlString = Client.DownloadString(ConfigUrl);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlString);

                    XmlNode defaultNode = xmlDoc.SelectSingleNode("//config/default");
                    XmlNode machineNode = xmlDoc.SelectSingleNode(string.Format("//config/nodes/node[@name='{0}']", Hostname));

                    configNode.Configure(machineNode);
                    configNode.Parent.Configure(defaultNode);
                }
                catch { }
            }
        }

        private string GetHostName()
        {
            string name = null; ;

            try
            {
                name = Convert.ToBase64String(Encoding.ASCII.GetBytes(Environment.MachineName));
            }
            catch { }

            if (string.IsNullOrEmpty(name))
            {
                try
                {
                    foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (nic.OperationalStatus == OperationalStatus.Up)
                        {
                            name = Convert.ToBase64String(nic.GetPhysicalAddress().GetAddressBytes());
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "unnamed";
            }

            return name.Replace("=", "");
        }
    }
}
