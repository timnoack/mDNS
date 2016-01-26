using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mDNS.Logging
{
    public class LogMessageEventArgs : EventArgs
    {
        public object[] Content { private set; get; }
        public LogType Type { private set; get; }
        public string Source { private set; get; }

        public LogMessageEventArgs(object[] content, LogType type, string source)
        {
            this.Content = content;
            this.Type = type;
            this.Source = source;
        }

        public override string ToString()
        {
            return "[" + this.Type + "] \"" + this.Source + "\": " + string.Join<object>("; ", Content);
        }
    }

    public enum LogType
    {
        Warn,
        Debug,
        Info,
        Error
    }
}
