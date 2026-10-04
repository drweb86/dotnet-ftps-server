using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FtpsServerLibrary;

public interface IFtpsServerFileSystemProvider
{
    Task CreateDirectory(string userFolder, IEnumerable<string> parts);
    
    Task<bool> DirectoryExists(string userFolder, IEnumerable<string> parts);
    Task<bool> FileExists(string userFolder, IEnumerable<string> parts);
    
    Task DirectoryDelete(string userFolder, IEnumerable<string> parts);
    Task FileDelete(string userFolder, IEnumerable<string> parts);

    Task DirectoryMove(string userFolder, IEnumerable<string> fromParts, IEnumerable<string> toParts);
    Task FileMove(string userFolder, IEnumerable<string> fromParts, IEnumerable<string> toParts);


    Task<Stream> FileCreate(string userFolder, IEnumerable<string> parts);
    Task<Stream> FileOpenRead(string userFolder, IEnumerable<string> parts);
    Task<DateTime> GetFileLastWriteTimeUtc(string userFolder, IEnumerable<string> parts);
    Task<long> GetFileLength(string userFolder, IEnumerable<string> parts);

    Task<string> GetFileName(string pickerFile);

    Task<IEnumerable<FtpsServerFileSystemEntry>> DirectoryGetFileSystemEntries(string userFolder, IEnumerable<string> parts);
}

public class FtpsServerFileSystemProvider: IFtpsServerFileSystemProvider
{
    public Task<Stream> FileCreate(string userFolder, IEnumerable<string> parts)
    {
        var file = GetRealPath(userFolder, parts);
        Stream stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
        return Task.FromResult(stream);
    }

    public Task<Stream> FileOpenRead(string userFolder, IEnumerable<string> parts)
    {
        var file = GetRealPath(userFolder, parts);
        Stream stream = File.OpenRead(file);
        return Task.FromResult(stream);
    }

    public Task<DateTime> GetFileLastWriteTimeUtc(string userFolder, IEnumerable<string> parts)
    {
        var file = GetRealPath(userFolder, parts);
        var fileInfo = new FileInfo(file);
        return Task.FromResult(fileInfo.LastWriteTimeUtc);
    }

    public Task<long> GetFileLength(string userFolder, IEnumerable<string> parts)
    {
        var file = GetRealPath(userFolder, parts);
        var fileInfo = new FileInfo(file);
        return Task.FromResult(fileInfo.Length);
    }

    public Task CreateDirectory(string userFolder, IEnumerable<string> parts)
    {
        var actualFolder = GetRealPath(userFolder, parts);
        Directory.CreateDirectory(actualFolder);
        return Task.CompletedTask;
    }

    public Task<bool> DirectoryExists(string userFolder, IEnumerable<string> parts)
    {
        var actualFolder = GetRealPath(userFolder, parts);
        return Task.FromResult(Directory.Exists(actualFolder));
    }

    public Task<bool> FileExists(string userFolder, IEnumerable<string> parts)
    {
        var actualFolder = GetRealPath(userFolder, parts);
        return Task.FromResult(File.Exists(actualFolder));
    }

    public Task DirectoryDelete(string userFolder, IEnumerable<string> parts)
    {
        var actualFolder = GetRealPath(userFolder, parts);
        Directory.Delete(actualFolder, true);
        return Task.CompletedTask;
    }

    public Task FileDelete(string userFolder, IEnumerable<string> parts)
    {
        var actualFile = GetRealPath(userFolder, parts);
        File.Delete(actualFile);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<FtpsServerFileSystemEntry>> DirectoryGetFileSystemEntries(string userFolder, IEnumerable<string> parts)
    {
        var folder = GetRealPath(userFolder, parts);
        var result = new List<FtpsServerFileSystemEntry>();

        var folderInfo = new DirectoryInfo(folder);
        var parentFolderInfo = folderInfo;
        if (parts.Any())
            parentFolderInfo = folderInfo.Parent ?? folderInfo;

        result.Add(new FtpsServerFileSystemEntry(".", folderInfo.LastWriteTimeUtc, 0, true));
        result.Add(new FtpsServerFileSystemEntry("..", parentFolderInfo.LastWriteTimeUtc, 0, true));


        result.AddRange(Directory
            .GetFileSystemEntries(folder)
            .Where(x => !IsHiddenSystemDirectory(x))
            .Select(x =>
            {
                var fullName = Path.Combine(folder, x);
                if (File.Exists(fullName))
                {
                    var info = new FileInfo(x);
                    return new FtpsServerFileSystemEntry(Path.GetFileName(x), info.LastWriteTimeUtc, info.Length, false);
                }
                else
                {
                    var info = new DirectoryInfo(x);
                    return new FtpsServerFileSystemEntry(Path.GetFileName(x), info.LastWriteTimeUtc, 0, true);
                }
            }));

        IEnumerable<FtpsServerFileSystemEntry> result2 = result;
        return Task.FromResult(result2);
    }

    public Task<string> GetFileName(string pickerFile)
    {
        return Task.FromResult(pickerFile);
    }

    // Handle $RECYCLE.BIN, System Volume Information
    private static bool IsHiddenSystemDirectory(string path)
    {
        if (!OperatingSystem.IsWindows() || !Directory.Exists(path))
            return false;

        return (new DirectoryInfo(path).Attributes & FileAttributes.System) == FileAttributes.System;
    }

    private string GetRealPath(string userFolder, params IEnumerable<string> parts)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(userFolder, nameof(userFolder));

        // Normalize the base path
        string normalizedBase = Path.GetFullPath(userFolder);

        // Ensure base path ends with directory separator
        if (!normalizedBase.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !normalizedBase.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            normalizedBase += Path.DirectorySeparatorChar;
        }

        // Convert virtual path to system-appropriate path
        string virtualPathStr = string.Join("/", parts);

        // Replace / with system directory separator
        virtualPathStr = virtualPathStr.Replace('/', Path.DirectorySeparatorChar);

        // Combine paths
        if (Path.IsPathRooted(virtualPathStr))
            throw new UnauthorizedAccessException($"{virtualPathStr} path rooted path supplied");
        string combinedPath = Path.Combine(normalizedBase, virtualPathStr);

        // Get the full normalized path
        string fullPath = Path.GetFullPath(combinedPath);
        if (userFolder.Contains('\\'))
            fullPath = fullPath.Replace('/', '\\');
        else
            fullPath = fullPath.Replace('\\', '/');


        // Security check: ensure the result is within the base path
        if (!IsInsideBase(normalizedBase, fullPath))
        {
            throw new UnauthorizedAccessException(
                $"Access denied. The path '{ToString()}' attempts to escape the base directory '{userFolder}'.");
        }

        fullPath = ResolveLinks(userFolder, normalizedBase, fullPath);
        return ToExtendedLengthPath(fullPath);
    }

    // A symlink or junction keeps a name inside the share, so the text check above accepts it.
    // Walk each existing part, and reject a link whose target is outside the user folder.
    private static string ResolveLinks(string userFolder, string normalizedBase, string fullPath)
    {
        var relative = fullPath.Length > normalizedBase.Length ? fullPath[normalizedBase.Length..] : "";
        if (relative.Length == 0)
            return fullPath;

        var current = normalizedBase.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var segment in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
            current = FollowLink(userFolder, normalizedBase, Path.Combine(current, segment));

        return current;
    }

    private static string FollowLink(string userFolder, string normalizedBase, string path)
    {
        var current = path;
        for (var hop = 0; hop < 32; hop++)
        {
            FileAttributes attributes;
            try
            {
                attributes = File.GetAttributes(current);
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                return current;
            }

            if ((attributes & FileAttributes.ReparsePoint) == 0)
                return current;

            var isDirectory = (attributes & FileAttributes.Directory) != 0;
            var link = isDirectory
                ? Directory.ResolveLinkTarget(current, returnFinalTarget: false)
                : File.ResolveLinkTarget(current, returnFinalTarget: false);
            if (link is null)
                return current;

            var target = AlignSlashes(userFolder, Path.GetFullPath(link.FullName));
            if (!IsInsideBase(normalizedBase, target))
            {
                throw new UnauthorizedAccessException(
                    $"Access denied. The link '{current}' points outside the base directory '{normalizedBase}'.");
            }

            current = target.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        throw new UnauthorizedAccessException($"Access denied. The link '{path}' is too deeply nested.");
    }

    private static string AlignSlashes(string userFolder, string path)
    {
        if (userFolder.Contains('\\'))
            return path.Replace('/', '\\');
        return path.Replace('\\', '/');
    }

    private static bool IsInsideBase(string normalizedBase, string fullPath)
    {
        if (fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            return true;

        var trimmedBase = normalizedBase.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var trimmedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(trimmedPath, trimmedBase, StringComparison.OrdinalIgnoreCase);
    }

    // The \\?\ prefix makes Windows use the path as a file name, so CON.txt is not the CON device.
    private static string ToExtendedLengthPath(string fullPath)
    {
        if (!OperatingSystem.IsWindows())
            return fullPath;
        if (fullPath.StartsWith(@"\\?\", StringComparison.Ordinal))
            return fullPath;

        var native = fullPath.Replace('/', '\\');
        if (native.StartsWith(@"\\", StringComparison.Ordinal))
            return @"\\?\UNC\" + native[2..];
        if (native.Length >= 3 && native[1] == ':' && native[2] == '\\')
            return @"\\?\" + native;
        return fullPath;
    }

    public Task<string> ResolveUserFolder(string userFolder)
    {
        return Task.FromResult(new DirectoryInfo(userFolder).FullName);
    }

    public Task DirectoryMove(string userFolder, IEnumerable<string> fromParts, IEnumerable<string> toParts)
    {
        var from = GetRealPath(userFolder, fromParts);
        var to = GetRealPath(userFolder, toParts);

        Directory.Move(from, to);

        return Task.CompletedTask;
    }

    public Task FileMove(string userFolder, IEnumerable<string> fromParts, IEnumerable<string> toParts)
    {
        var from = GetRealPath(userFolder, fromParts);
        var to = GetRealPath(userFolder, toParts);

        Directory.Move(from, to);

        return Task.CompletedTask;
    }
}