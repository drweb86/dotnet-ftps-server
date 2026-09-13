[Languages](README.md)

# 隐私政策

最近更新日期：2026年9月13日


**FTPS Server** by Siarhei Kuchuk

应用名称: FTPS Server
开发者姓名: Siarhei Kuchuk

本软件是本地 FTPS（基于 TLS 的 FTP）服务器。它不会创建云账号。
开发者不会运营用于接收您的文件、密码或使用数据的后台服务。

## 开发者不收集的数据

本应用不含广告、分析、崩溃上报或跟踪 SDK。开发者不收集、出售或共享个人数据。

## 存储在您计算机上的数据

应用设置（包括 FTPS 用户名和密码、服务器端口、连接限制以及可选的证书路径和密码）仅保存在本计算机上：

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

如果应用创建自签名证书，则存储于此：

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

服务器日志可能写入于此：

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

这些值不会上传给开发者。卸载应用或删除这些文件夹会将其删除。共享的 **文件** 仍保留在您选择的文件夹中；应用不会将它们复制到开发者服务器。

文件夹通过系统文件夹选择器选定。应用只会共享您授权的文件夹。

不使用开发者的服务器存储您的数据。

## 网络使用

### 更新检查

应用可能会请求 GitHub 上的最新版本：

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub（Microsoft）会收到普通的 HTTPS 请求（IP 地址、user-agent、时间）。开发者不会收到该流量。

### FTPS 服务器

服务器运行时，会在您的本地网络上监听，以便您配置的 FTPS 客户端使用您设置的用户名和密码，读取或写入您共享的文件夹。该流量仅存在于您的设备之间（以及网络上持有这些凭据的任何人）。开发者不是其中一方。

您负责谁可以访问该端口、共享哪些文件夹，以及这些密码的强度。

### 您打开的链接

应用可在系统浏览器中打开这些页面。这些网站有各自的隐私政策：

- 项目主页: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- 许可证: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- 最新版本: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## 其他本地行为

服务器运行时，应用可能会请求操作系统减少休眠，以便传输继续进行。

## 儿童

本应用是网络文件服务器，不以 13 岁以下儿童为对象。

## 第三方

GitHub 处理更新检查请求以及您打开的页面，如上所述。开发者不会收到该流量。

## 变更

对本政策的更新将发布在项目仓库的本文件中。

## 联系

应用名称: FTPS Server
开发者姓名: Siarhei Kuchuk

问题： [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
