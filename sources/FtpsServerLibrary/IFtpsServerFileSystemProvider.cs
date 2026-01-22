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
    Task<string> ResolveUserFolder(string userFolder);
}

public class FtpsServerFileSystemProvider: IFtpsServerFileSystemProvider
{
    public Task<Stream> FileCreate(string userFolder, IEnumerable<string> parts)
    {
        var file = GetRealPath(userFolder, parts);
        Stream stream = File.OpenWrite(file);
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

        result.Add(new FtpsServerFileSystemEntry(".", folderInfo.LastWriteTime, 0, true));
        result.Add(new FtpsServerFileSystemEntry("..", parentFolderInfo.LastWriteTime, 0, true));


        result.AddRange(Directory
            .GetFileSystemEntries(folder)
            .Select(x => 
            {
                var fullName = Path.Combine(folder, x);
                if (File.Exists(fullName))
                {
                    var info = new FileInfo(x);
                    return new FtpsServerFileSystemEntry(Path.GetFileName(x), info.LastWriteTime, info.Length, false);
                } 
                else
                {
                    var info = new DirectoryInfo(x);
                    return new FtpsServerFileSystemEntry(Path.GetFileName(x), info.LastWriteTime, 0, true);
                }
            }));

        IEnumerable<FtpsServerFileSystemEntry> result2 = result;
        return Task.FromResult(result2);
    }

    public Task<string> GetFileName(string pickerFile)
    {
        return Task.FromResult(pickerFile);
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
        if (!fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Access denied. The path '{ToString()}' attempts to escape the base directory '{userFolder}'.");
        }

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