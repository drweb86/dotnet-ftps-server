using System.Net;
using System.Net.NetworkInformation;

namespace FtpsServerApp;

class NetworkInfo
{
    public NetworkInfo(NetworkInterface networkInterface)
    {
        Name = networkInterface.Name;
        Description = networkInterface.Description;
        InterfaceType = networkInterface.NetworkInterfaceType;
        Addresses = new List<IPAddress>();
    }
    public string Name { get; set; }
    public string Description { get; set; }
    public NetworkInterfaceType InterfaceType { get; set; }

    public List<IPAddress> Addresses { get; set; }
}
