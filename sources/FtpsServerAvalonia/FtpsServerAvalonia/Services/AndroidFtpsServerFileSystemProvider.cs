using Avalonia;
using Avalonia.Platform.Storage;
using FtpsServerLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FtpsServerAvalonia.Services;

public class AndroidFtpsServerFileSystemProvider : IFtpsServerFileSystemProvider
{
    private readonly Dictionary<string, IStorageFolder> _folderCache = new();
    private readonly IStorageProvider _storageProvider;

    public AndroidFtpsServerFileSystemProvider(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    private async Task<IStorageFolder> GetRootFolder(string folderBookmark)
    {
        if (_folderCache.TryGetValue(folderBookmark, out var cachedFolder))
            return cachedFolder;

        var folder = await _storageProvider.TryGetFolderFromPathAsync(new Uri(folderBookmark));

        if (folder == null)
            throw new DirectoryNotFoundException($"Cannot access folder: {folderBookmark}");

        _folderCache[folderBookmark] = folder;
        return folder;
    }

    private async Task<IStorageFolder> NavigateToFolder(string folderBookmark, IEnumerable<string> parts)
    {
        var currentFolder = await GetRootFolder(folderBookmark);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part) || part == ".")
                continue;

            if (part == "..")
            {
                var parent = await currentFolder.GetParentAsync();
                if (parent != null)
                    currentFolder = parent;
                continue;
            }

            var items = await currentFolder.GetItemsAsync().ToListAsync();
            var nextFolder = items.OfType<IStorageFolder>().FirstOrDefault(f => f.Name == part);

            if (nextFolder == null)
                throw new DirectoryNotFoundException($"Directory not found: {part}");

            currentFolder = nextFolder;
        }

        return currentFolder;
    }

    private async Task<IStorageFile> NavigateToFile(string folderBookmark, IEnumerable<string> parts)
    {
        var partsList = parts.ToList();
        if (partsList.Count == 0)
            throw new FileNotFoundException("File path is empty");

        var fileName = partsList[^1];
        var folderParts = partsList.Take(partsList.Count - 1);

        var folder = await NavigateToFolder(folderBookmark, folderParts);
        var items = await folder.GetItemsAsync().ToListAsync();
        var file = items.OfType<IStorageFile>().FirstOrDefault(f => f.Name == fileName);

        if (file == null)
            throw new FileNotFoundException($"File not found: {fileName}");

        return file;
    }

    public async Task CreateDirectory(string folderBookmark, IEnumerable<string> parts)
    {
        var partsList = parts.ToList();
        if (partsList.Count == 0)
            return;

        var newFolderName = partsList[^1];
        var parentParts = partsList.Take(partsList.Count - 1);

        var parentFolder = await NavigateToFolder(folderBookmark, parentParts);
        await parentFolder.CreateFolderAsync(newFolderName);
    }

    public async Task<bool> DirectoryExists(string folderBookmark, IEnumerable<string> parts)
    {
        try
        {
            await NavigateToFolder(folderBookmark, parts);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    public async Task<bool> FileExists(string folderBookmark, IEnumerable<string> parts)
    {
        try
        {
            await NavigateToFile(folderBookmark, parts);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public async Task DirectoryDelete(string folderBookmark, IEnumerable<string> parts)
    {
        var folder = await NavigateToFolder(folderBookmark, parts);
        await folder.DeleteAsync();
    }

    public async Task FileDelete(string folderBookmark, IEnumerable<string> parts)
    {
        var file = await NavigateToFile(folderBookmark, parts);
        await file.DeleteAsync();
    }

    public async Task DirectoryMove(string folderBookmark, IEnumerable<string> fromParts, IEnumerable<string> toParts)
    {
        var fromFolder = await NavigateToFolder(folderBookmark, fromParts);

        var toPartsList = toParts.ToList();
        var newName = toPartsList[^1];
        var toParentParts = toPartsList.Take(toPartsList.Count - 1);
        var toParentFolder = await NavigateToFolder(folderBookmark, toParentParts);

        await fromFolder.MoveAsync(toParentFolder);

        // Note: Avalonia's IStorageFolder doesn't have a rename method directly
        // The move operation should handle the rename if the destination has a different name
    }

    public async Task FileMove(string folderBookmark, IEnumerable<string> fromParts, IEnumerable<string> toParts)
    {
        var fromFile = await NavigateToFile(folderBookmark, fromParts);

        var toPartsList = toParts.ToList();
        var newName = toPartsList[^1];
        var toParentParts = toPartsList.Take(toPartsList.Count - 1);
        var toParentFolder = await NavigateToFolder(folderBookmark, toParentParts);

        await fromFile.MoveAsync(toParentFolder);
    }

    public async Task<Stream> FileCreate(string folderBookmark, IEnumerable<string> parts)
    {
        var partsList = parts.ToList();
        if (partsList.Count == 0)
            throw new ArgumentException("File path is empty");

        var fileName = partsList[^1];
        var folderParts = partsList.Take(partsList.Count - 1);

        var folder = await NavigateToFolder(folderBookmark, folderParts);
        var file = await folder.CreateFileAsync(fileName);

        if (file == null)
            throw new IOException($"Failed to create file: {fileName}");

        return await file.OpenWriteAsync();
    }

    public async Task<Stream> FileOpenRead(string folderBookmark, IEnumerable<string> parts)
    {
        var file = await NavigateToFile(folderBookmark, parts);
        return await file.OpenReadAsync();
    }

    public async Task<DateTime> GetFileLastWriteTimeUtc(string folderBookmark, IEnumerable<string> parts)
    {
        var file = await NavigateToFile(folderBookmark, parts);
        var properties = await file.GetBasicPropertiesAsync();
        return properties.DateModified?.UtcDateTime ?? DateTime.UtcNow;
    }

    public async Task<long> GetFileLength(string folderBookmark, IEnumerable<string> parts)
    {
        var file = await NavigateToFile(folderBookmark, parts);
        var properties = await file.GetBasicPropertiesAsync();
        return (long)(properties.Size ?? 0);
    }

    public Task<string> GetFileName(string pickerFile)
    {
        // On Android, the pickerFile might be a URI, extract the file name from it
        if (Uri.TryCreate(pickerFile, UriKind.Absolute, out var uri))
        {
            var segments = uri.Segments;
            if (segments.Length > 0)
                return Task.FromResult(Uri.UnescapeDataString(segments[^1].TrimEnd('/')));
        }
        return Task.FromResult(Path.GetFileName(pickerFile));
    }

    public async Task<IEnumerable<FtpsServerFileSystemEntry>> DirectoryGetFileSystemEntries(string folderBookmark, IEnumerable<string> parts)
    {
        var folder = await NavigateToFolder(folderBookmark, parts);
        var result = new List<FtpsServerFileSystemEntry>();

        // Add current directory entry
        var folderProperties = await folder.GetBasicPropertiesAsync();
        result.Add(new FtpsServerFileSystemEntry(".", folderProperties.DateModified?.DateTime ?? DateTime.Now, 0, true));

        // Add parent directory entry
        var parent = await folder.GetParentAsync();
        if (parent != null)
        {
            var parentProperties = await parent.GetBasicPropertiesAsync();
            result.Add(new FtpsServerFileSystemEntry("..", parentProperties.DateModified?.DateTime ?? DateTime.Now, 0, true));
        }
        else
        {
            result.Add(new FtpsServerFileSystemEntry("..", folderProperties.DateModified?.DateTime ?? DateTime.Now, 0, true));
        }

        // Get all items in the folder
        var items = await folder.GetItemsAsync().ToListAsync();

        foreach (var item in items)
        {
            var properties = await item.GetBasicPropertiesAsync();
            var lastWriteTime = properties.DateModified?.DateTime ?? DateTime.Now;

            if (item is IStorageFile file)
            {
                var size = (long)(properties.Size ?? 0);
                result.Add(new FtpsServerFileSystemEntry(item.Name, lastWriteTime, size, false));
            }
            else if (item is IStorageFolder)
            {
                result.Add(new FtpsServerFileSystemEntry(item.Name, lastWriteTime, 0, true));
            }
        }

        return result;
    }

    public async Task<string> ResolveUserFolder(string folderBookmark)
    {
        var folder = await GetRootFolder(folderBookmark);
        return folder.Name;
    }
}
