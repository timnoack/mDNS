using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace mDNS
{
    public static class Dns
    {
        public static IPAddress GetLocalIp()
        {
            // http://stackoverflow.com/questions/10336521/query-local-ip-address
            var icp = NetworkInformation.GetInternetConnectionProfile();
            
            if (icp?.NetworkAdapter == null) return null;
            Windows.Networking.HostName hostname =
                NetworkInformation.GetHostNames()
                    .SingleOrDefault(
                        hn =>
                            hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);

            // the ip address
            return IPAddress.Parse(hostname?.CanonicalName);
        }

        public static string GetLocalHostName()
        {
            // http://stackoverflow.com/questions/32876966/how-to-get-local-host-name-in-c-sharp-on-a-windows-10-universal-app
            var hostNames = NetworkInformation.GetHostNames();
            return hostNames.FirstOrDefault(name => name.Type == HostNameType.DomainName)?.DisplayName;
        }
    
    }
}
