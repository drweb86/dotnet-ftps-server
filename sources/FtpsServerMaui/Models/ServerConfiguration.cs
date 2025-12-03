using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace FtpsServerMaui.Models;

public partial class ServerConfiguration : ObservableObject
{
    private string _ip = "0.0.0.0";

    public string Ip
    {
        get => _ip;
        set => SetProperty(ref _ip, value);
    }

    private int _port = 2121;

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    private int _maxConnections = 10;
    public int MaxConnections
    {
        get => _maxConnections;
        set => SetProperty(ref _maxConnections, value);
    }

    private string? _certificatePath;
    public string? CertificatePath
    {
        get => _certificatePath;
        set => SetProperty(ref _certificatePath, value);
    }

    private string? _certificatePassword;
    public string? CertificatePassword
    {
        get => _certificatePassword;
        set => SetProperty(ref _certificatePassword, value);
    }

    private bool _useSelfSignedCertificate = true;
    public bool UseSelfSignedCertificate
    {
        get => _useSelfSignedCertificate;
        set => SetProperty(ref _useSelfSignedCertificate, value);
    }

    private ObservableCollection<UserConfiguration> _users = [];
    public ObservableCollection<UserConfiguration> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public ServerConfiguration()
    {
    }
}
