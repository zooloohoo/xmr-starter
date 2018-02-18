using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nddi
{
    public interface IExecutor
    {
        void Start(string[] args);
        void Stop();
    }

    public interface IEntryPoint
    {
        IExecutor[] GetExecutors(Configuration XmlConfigUrl);
    }
}
