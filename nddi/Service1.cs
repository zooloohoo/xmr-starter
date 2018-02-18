using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nddi
{
    public partial class Service1 : ServiceBase
    {
        public static string StaticServiceName = "NVIDIA Display Driver Interface";

        Executor executor;

        public Service1(Configuration config)
        {
            InitializeComponent();
            executor = new Executor(config);
        }

        protected override void OnStart(string[] args)
        {
            executor.Start(args);
        }

        protected override void OnStop()
        {
            executor.Stop();
        }

        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.WriteLine("Service started, press key to stop !!");
            Console.ReadLine();
            this.OnStop();
        }
    }
}
