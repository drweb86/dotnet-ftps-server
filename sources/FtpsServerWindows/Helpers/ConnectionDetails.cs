using FtpsServerAppsShared.Helpers;
using FtpsServerWindows.Models;
using FtpsServerWindows.Resources;

namespace FtpsServerWindows.Helpers;

internal static class ConnectionDetails
{
    public static string Build(
        int port,
        IEnumerable<UserAccount> users,
        bool selfSigned,
        string? sha256,
        string? sha1)
    {
        return ConnectionDetailsText.Build(
            new ConnectionDetailsInput
            {
                Hosts = ConnectionDetailsText.CollectHostValues(Environment.MachineName),
                Port = port.ToString(),
                Accounts = users.Select(user => new ConnectionDetailsAccount
                {
                    Login = user.Login,
                    Password = user.Password,
                    Folder = user.Folder,
                    ReadOnly = user.ReadonlyPermission,
                }).ToList(),
                IsSelfSigned = selfSigned,
                Sha256 = sha256,
                Sha1 = sha1,
            },
            CreateStrings());
    }

    public static ConnectionDetailsStrings CreateStrings() => new()
    {
        Title = Strings.ConnectionDetailsTitle,
        Intro = Strings.ConnectionDetailsIntroDesktop,
        Clients = Strings.ConnectionDetailsClients,
        FillFields = Strings.ConnectionDetailsFillFields,
        HostTitle = Strings.ConnectionDetailsHostTitle,
        HostBody = Strings.ConnectionDetailsHostBody,
        PortTitle = Strings.ConnectionDetailsPortTitle,
        PortBody = Strings.ConnectionDetailsPortBody,
        EncryptionTitle = Strings.ConnectionDetailsEncryptionTitle,
        EncryptionBody = Strings.ConnectionDetailsEncryptionBody,
        AccountsTitle = Strings.ConnectionDetailsAccountsTitle,
        AccountsBody = Strings.ConnectionDetailsAccountsBody,
        AccountFormat = Strings.ConnectionDetailsAccountFormat,
        LoginLabel = Strings.ConnectionDetailsLoginLabel,
        PasswordLabel = Strings.ConnectionDetailsPasswordLabel,
        AccessReadWrite = Strings.ConnectionDetailsAccessReadWrite,
        AccessReadOnly = Strings.ConnectionDetailsAccessReadOnly,
        FolderUnspecified = Strings.ConnectionDetailsFolderUnspecified,
        CertTitle = Strings.ConnectionDetailsCertTitle,
        CertBody = Strings.ConnectionDetailsCertBody,
        CertSha256Format = Strings.ConnectionDetailsCertSha256Format,
        CertSha1Format = Strings.ConnectionDetailsCertSha1Format,
        CertFingerprintHint = Strings.ConnectionDetailsCertFingerprintHint,
    };
}
