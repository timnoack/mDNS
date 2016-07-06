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
        public static IPAddress GetLocalIp(bool ipv6)
        {
            var hosts = NetworkInformation.GetHostNames()
                .Where(hn => hn.Type == (ipv6 ? HostNameType.Ipv6 : HostNameType.Ipv4));

            if (!hosts.Any()) // no host found with version of ip-address in mind
                return GetInternetIP();

            if (hosts.Count() == 1) // success
                return IPAddress.Parse(hosts.First().CanonicalName);

            // multiple hosts found
            var icp = NetworkInformation.GetInternetConnectionProfile();
            var internetHost = hosts.SingleOrDefault(hn => hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);
            if (internetHost != null)
                return IPAddress.Parse(internetHost.CanonicalName);

            // internet host has wrong ip-version, return first (random) host
            return IPAddress.Parse(hosts.First().CanonicalName);

        }

        public static IPAddress GetInternetIP()
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
