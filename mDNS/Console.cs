using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mDNS
{
    public static class Console
    {
        public static void WriteLine(params object[] args)
        {
            global::mDNS.Logging.LogManager.GetLogger("Console").Info(args);
        }
    }
}
