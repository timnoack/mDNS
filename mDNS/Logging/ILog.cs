using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mDNS.Logging
{
    public interface ILog
    {
        void Warn(params object[] args);
        void Debug(params object[] args);
        void Info(params object[] args);
        void Error(params object[] args);

        event EventHandler<LogMessageEventArgs> MessageReceived;
    }
}
