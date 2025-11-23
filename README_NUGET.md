## Enhanced C# FTPS .Net 10 Server & Library

A FTPS (FTP over TLS) server implementation in C# with logging, user permissions, and flexible configuration options. Aimed for Windows, Linux, or macOS platforms. Does not have dependencies.

This is a sample implementation provided for:
- ✅ Learning and education
- ✅ Internal corporate use
- ✅ Personal projects
- ✅ Modification and customization

Not recommended for:
- ⚠️ Internet-facing production without security review
- ⚠️ Mission-critical systems without testing
- ⚠️ Compliance-required environments without audit

## Features

✅ **User Permissions** - Granular control over Read/Write operations
✅ **Per-User Root Folders** - Isolated directories for each user
✅ **TLS/SSL Encryption** - Secure FTPS connections
✅ **Full FTP Protocol** - All standard FTP commands supported
✅ **Path Security** - Protection against directory traversal attacks

## Prerequisites

- .NET 10.0 SDK.

## ⚙️ Configuration

See 
- https://github.com/drweb86/dotnet-ftps-server/blob/main/sources/FtpsServerLibrary/FtpsServerSettings.cs and 
- https://github.com/drweb86/dotnet-ftps-server/blob/main/sources/FtpsServerLibrary/FtpsServerUserAccount.cs .


| Parameter                               | Required  | Default value | Remarks                                                                                                                 |
|-----------------------------------------|-----------|---------------|-------------------------------------------------------------------------------------------------------------------------|
| ServerSettings.Ip                       | No        | 0.0.0.0       | The IP address server will be listening to. 0.0.0.0 - listen on every available network interface.                      |
| ServerSettings.Port                     | No        | 2121          | The Port for server to listen to.                                                                                       |
| ServerSettings.MaxConnections           | No        | 10            | Maximum number of simultaneous server connections.                                                                      |
| ServerSettings.CertificatePath          | No        |               | PEM, DER or PKCS#12 PFX file. PFX file is opened with CertificatePassword (if specified).                               |
| ServerSettings.CertificatePassword      | No        |               | Certificate password. When specified, will be used for opening certificate from CertificatePath.                        |
| ServerSettings.CertificateStoreName     | No        |               | Certificate store name. Possible values: AuthRoot, CertificateAuthority, My, Root, TrustedPublisher. Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified. |
| ServerSettings.CertificateStoreLocation | No        |               | Certificate store location. Possible values: CurrentUser, LocalMachine. Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified.                              |
| ServerSettings.CertificateStoreSubject  | No        |               | Certificate store subject by which certificate will be searched in certificate store and location. Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified.   |
| Users[].Login                           | Yes       |               | User's login.                                                                   |
| Users[].Password                        | Yes       |               | User's password.                                                                |
| Users[].Folder                          | Yes       |               | User's folder.                                                                   |
| Users[].Read                            | Yes       |               | Can user read folder contents and download files.                               |
| Users[].Write                           | Yes       |               | Can user create, upload, write, delete, rename operations on files and folders. |

If certificate is not specified, self-signed certificate will be created and stored in %localappdata%\FtpsServerLibrary\Certificates.

## 🚀 Run

```
using FtpsServerLibrary;

var config = new FtpsServerConfiguration
{
    ServerSettings = new FtpsServerSettings { },
    Users = [
        new FtpsServerUserAccount { Folder = @"c:\folder", Login = "admin", Password = "admin", Read = true, Write = true}
    ]
};
var server = new FtpsServer(new StubLog(), config);
server.Start();
Console.WriteLine("Server is started at 0.0.0.0 listening on port 2121 with self-signed certificate");
Console.ReadLine();
server.Stop();

class StubLog : IFtpsServerLog
{
    public void Debug(string message) { }
    public void Error(Exception ex, string message) { }
    public void Fatal(Exception ex, string message) { }
    public void Info(string message) { }
    public void Warn(string message) { }
}
```
