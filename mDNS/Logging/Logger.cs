using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mDNS.Logging
{
    public class Logger : ILog
    {
        public string Name;

        public event EventHandler<LogMessageEventArgs> MessageReceived;

        public Logger(string name)
        {
            this.Name = name;
        }

        public void Debug(params object[] args)
        {
            MessageReceived(this, new LogMessageEventArgs(args, LogType.Debug, this.Name));  
        }

        public void Info(params object[] args)
        {
            MessageReceived(this, new LogMessageEventArgs(args, LogType.Info, this.Name));
        }

        public void Warn(params object[] args)
        {
            MessageReceived(this, new LogMessageEventArgs(args, LogType.Warn, this.Name));
        }

        public void Error(params object[] args)
        {
            MessageReceived(this, new LogMessageEventArgs(args, LogType.Error, this.Name));
        }
    }
}
