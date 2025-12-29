# Sharing files via FTPS betweeen devices over network.

FTPS Server application is written in C# .Net-Core 10 for Windows and Ubuntu.

Features:

- **User Permissions** - Granular control over Read/Write operations
- **Per-User Root Folders** - Isolated directories for each user
- **Path Security** - Protection against directory traversal attacks

<img width="1109" height="614" alt="image" src="https://github.com/user-attachments/assets/da502ae9-01ae-4bfe-9619-653d6395067b" />

<img width="760" height="433" alt="image" src="https://github.com/user-attachments/assets/37f8d159-9188-4838-83d4-4ae1b64b7b65" />

## 🎯 Use Cases

- 1. Exchange of files between PC and notebook over WI-FI.
- 2. Access to PC files from Android file manager over WI-FI.

| Component                      |                                                                                   |
|--------------------------------|-----------------------------------------------------------------------------------|
| [Library](./README_NUGET.md)   | [NUGet Package](https://www.nuget.org/packages/Siarhei_Kuchuk.FtpsServerLibrary)  |
| [UI for Windows and Ubuntu](./README_UI.md) |                                                                      |
| [Console](./README_CONSOLE.md) |                                                                                   |

[Connecting to the Server](./Connecting.md)

[Troubleshooting](./Troubleshooting.md)
