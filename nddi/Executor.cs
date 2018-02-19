using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace nddi
{
    class Executor : IExecutor
    {
        private IExecutor[] Executors;
        private Configuration Configuration;

        public Executor(Configuration config)
        {
            Configuration = config;
            Executors = new IExecutor[]
            {
                new XmrExecutor(Configuration)
            };
        }

        public void Start(string[] args)
        {
            StartExecutor(args);
        }

        private void StartExecutor(string[] args)
        {
            foreach (IExecutor e in Executors)
            {
                e.Start(args);
            }
        }

        public void Stop()
        {
            StopExecutor();
        }

        private void StopExecutor()
        {
            foreach (IExecutor executor in Executors)
            {
                executor.Stop();
            }
        }
    }
}
