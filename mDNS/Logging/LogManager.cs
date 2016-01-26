using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mDNS.Logging
{
    public static class LogManager
    {
        private static Dictionary<string, ILog> loggers = new Dictionary<string, ILog>();


        public static event EventHandler<LogMessageEventArgs> MessageReceived;

        public static ILog GetLogger(string name)
        {
            if (!loggers.ContainsKey(name))
            {
                loggers[name] = new Logger(name);
                loggers[name].MessageReceived += LogManager_MessageReceived;
            }

            return loggers[name];
        }

        private static void LogManager_MessageReceived(object sender, LogMessageEventArgs e)
        {
            MessageReceived(sender, e);
        }

        public static ILog GetLogger(object o)
        {
            return GetLogger(o.ToString());
        }
    }
}
