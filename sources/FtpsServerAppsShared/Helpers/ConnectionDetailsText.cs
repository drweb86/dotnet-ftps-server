using System.Net;
using System.Text;
using FtpsServerApp.Helpers;

namespace FtpsServerAppsShared.Helpers;

public sealed class ConnectionDetailsAccount
{
    public required string Login { get; init; }
    public required string Password { get; init; }
    public required string Folder { get; init; }
    public required bool ReadOnly { get; init; }
}

public sealed class ConnectionDetailsInput
{
    public required IReadOnlyList<string> Hosts { get; init; }
    public required string Port { get; init; }
    public required IReadOnlyList<ConnectionDetailsAccount> Accounts { get; init; }
    public bool IsSelfSigned { get; init; }
    public string? Sha256 { get; init; }
    public string? Sha1 { get; init; }
}

public sealed class ConnectionDetailsStrings
{
    public required string Title { get; init; }
    public required string Intro { get; init; }
    public required string Clients { get; init; }
    public required string FillFields { get; init; }
    public required string HostTitle { get; init; }
    public required string HostBody { get; init; }
    public required string PortTitle { get; init; }
    public required string PortBody { get; init; }
    public required string EncryptionTitle { get; init; }
    public required string EncryptionBody { get; init; }
    public required string AccountsTitle { get; init; }
    public required string AccountsBody { get; init; }
    public required string AccountFormat { get; init; }
    public required string LoginLabel { get; init; }
    public required string PasswordLabel { get; init; }
    public required string AccessReadWrite { get; init; }
    public required string AccessReadOnly { get; init; }
    public required string FolderUnspecified { get; init; }
    public required string CertTitle { get; init; }
    public required string CertBody { get; init; }
    public required string CertSha256Format { get; init; }
    public required string CertSha1Format { get; init; }
    public required string CertFingerprintHint { get; init; }
}

public sealed class ConnectionDetailsRequestEventArgs : EventArgs
{
    public string Text { get; set; } = "";
}

public static class ConnectionDetailsText
{
    public static IReadOnlyList<string> CollectHostValues(string? hostName)
    {
        var hosts = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var nic in NetworkHelper.GetMyLocalIps())
        {
            foreach (var addr in nic.Addresses)
            {
                if (IPAddress.IsLoopback(addr) || addr.IsIPv6LinkLocal)
                    continue;
                var value = addr.ToString();
                if (seen.Add(value))
                    hosts.Add(value);
            }
        }

        if (!string.IsNullOrWhiteSpace(hostName) && seen.Add(hostName))
            hosts.Add(hostName);

        return hosts;
    }

    public static string Build(ConnectionDetailsInput input, ConnectionDetailsStrings strings)
    {
        var b = new StringBuilder();
        void Line(string? text = null)
        {
            if (text != null)
                b.Append(text);
            b.Append('\n');
        }

        Line(strings.Title);
        Line();
        Line(strings.Intro);
        Line();
        Line(strings.Clients);
        Line();
        Line(strings.FillFields);
        Line();
        Line(strings.HostTitle);
        Line();
        Line(strings.HostBody);
        Line();
        foreach (var host in input.Hosts)
            Line("  " + host);
        Line();
        Line(strings.PortTitle);
        Line();
        Line(strings.PortBody);
        Line();
        Line("  " + input.Port);
        Line();
        Line(strings.EncryptionTitle);
        Line();
        Line(strings.EncryptionBody);
        Line();
        Line(strings.AccountsTitle);
        Line();
        Line(strings.AccountsBody);
        Line();
        for (var i = 0; i < input.Accounts.Count; i++)
        {
            var account = input.Accounts[i];
            var letter = i < 26 ? ((char)('a' + i)).ToString() : (i + 1).ToString();
            var access = account.ReadOnly ? strings.AccessReadOnly : strings.AccessReadWrite;
            var folder = string.IsNullOrWhiteSpace(account.Folder) ? strings.FolderUnspecified : account.Folder;
            Line("  " + string.Format(strings.AccountFormat, letter, account.Login, access, folder));
            Line();
            Line($"     {strings.LoginLabel}     {account.Login}");
            Line($"     {strings.PasswordLabel}  {account.Password}");
            Line();
        }

        if (input.IsSelfSigned
            && !string.IsNullOrWhiteSpace(input.Sha256)
            && !string.IsNullOrWhiteSpace(input.Sha1))
        {
            Line(strings.CertTitle);
            Line();
            Line(strings.CertBody);
            Line();
            Line("  " + string.Format(strings.CertSha256Format, input.Sha256));
            Line();
            Line("  " + string.Format(strings.CertSha1Format, input.Sha1));
            Line();
            Line(strings.CertFingerprintHint);
        }

        return b.ToString().TrimEnd() + "\n";
    }
}
