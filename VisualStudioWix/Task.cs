using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace VisualStudioWix
{
    public class Task : ITask
    {
        private IBuildEngine engine;
        public IBuildEngine BuildEngine
        {
            get { return engine; }
            set { engine = value; }
        }


        private ITaskHost host;
        public ITaskHost HostObject
        {
            get { return host; }
            set { host = value; }
        }

        public bool Execute()
        {
            return true;
        }
    }
}
