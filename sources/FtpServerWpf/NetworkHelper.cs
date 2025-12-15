using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FtpsServerApp;

internal static class NetworkHelper
{
    public static IEnumerable<NetworkInfo> GetMyLocalIps()
    {
        var items = new List<NetworkInfo>();
        foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (netInterface.OperationalStatus == OperationalStatus.Down)
                continue;

            var item = new NetworkInfo(netInterface);
            

            IPInterfaceProperties ipProps = netInterface.GetIPProperties();
            foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                item.Addresses.Add(addr.Address);

            if (item.Addresses.Any())
            {
                items.Add(item);
                item.Addresses = item.Addresses.OrderBy(x => x.AddressFamily).ToList();
            }
        }

        return items;
    }

    public static IPAddress[] GetIPsByName(string hostName, bool ip4Wanted, bool ip6Wanted)
    {
        if (IPAddress.TryParse(hostName, out var outIpAddress) == true)
            return new IPAddress[] { outIpAddress };

        IPAddress[] addresslist = Dns.GetHostAddresses(hostName);

        if (addresslist == null || addresslist.Length == 0)
            return new IPAddress[0];

        if (ip4Wanted && ip6Wanted)
            return addresslist;

        if (ip4Wanted)
            return addresslist.Where(o => o.AddressFamily == AddressFamily.InterNetwork).ToArray();

        if (ip6Wanted)
            return addresslist.Where(o => o.AddressFamily == AddressFamily.InterNetworkV6).ToArray();

        return new IPAddress[0];
    }
}
