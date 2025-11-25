using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace FtpsServerMaui.Models;

public partial class ServerConfiguration : ObservableObject
{
    [ObservableProperty]
    private string _ip = "0.0.0.0";

    [ObservableProperty]
    private int _port = 2121;

    [ObservableProperty]
    private int _maxConnections = 10;

    [ObservableProperty]
    private string? _certificatePath;

    [ObservableProperty]
    private string? _certificatePassword;

    [ObservableProperty]
    private bool _useSelfSignedCertificate = true;

    [ObservableProperty]
    private ObservableCollection<UserConfiguration> _users = new();

    public ServerConfiguration()
    {
    }
}
