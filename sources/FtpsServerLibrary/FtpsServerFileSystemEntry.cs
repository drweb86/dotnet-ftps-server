using System;

namespace FtpsServerLibrary;

public record FtpsServerFileSystemEntry(
    string FileName,
    DateTime LastWriteTime,
    long Length,
    bool IsDirectory);
