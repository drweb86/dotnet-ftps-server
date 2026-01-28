using Avalonia;
using Avalonia.Platform.Storage;
using FtpsServerLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FtpsServerAvalonia.Services;

public class AndroidFtpsServerFileSystemProvider(IStorageProvider storageProvider) : IFtpsServerFileSystemProvider
{
    private readonly Dictionary<string, IStorageFolder> _folderCache = [];
    private readonly IStorageProvider _storageProvider = storageProvider;

    private async Task<IStorageFolder> GetRootFolder(string serializedFolderBookmark)
    {
        if (_folderCache.TryGetValue(serializedFolderBookmark, out var cachedFolder))
            return cachedFolder;

        var bookmark = AndroidFolderBookmarkSerializer.Deserialise(serializedFolderBookmark);
        var folder = await _storageProvider.OpenFolderBookmarkAsync(bookmark.Bookmark) ?? throw new DirectoryNotFoundException($"Cannot access folder: {serializedFolderBookmark}");
        _folderCache[serializedFolderBookmark] = folder;
        return folder;
    }

    private async Task<IStorageFolder> NavigateToFolder(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var currentFolder = await GetRootFolder(serializedFolderBookmark);

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
            var nextFolder = items.OfType<IStorageFolder>().FirstOrDefault(f => f.Name == part) ?? throw new DirectoryNotFoundException($"Directory not found: {part}");
            currentFolder = nextFolder;
        }

        return currentFolder;
    }

    private async Task<IStorageFile> NavigateToFile(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var partsList = parts.ToList();
        if (partsList.Count == 0)
            throw new FileNotFoundException("File path is empty");

        var fileName = partsList[^1];
        var folderParts = partsList.Take(partsList.Count - 1);

        var folder = await NavigateToFolder(serializedFolderBookmark, folderParts);
        var items = await folder.GetItemsAsync().ToListAsync();
        var file = items.OfType<IStorageFile>().FirstOrDefault(f => f.Name == fileName) ?? throw new FileNotFoundException($"File not found: {fileName}");

        return file;
    }

    public async Task CreateDirectory(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var partsList = parts.ToList();
        if (partsList.Count == 0)
            return;

        var newFolderName = partsList[^1];
        var parentParts = partsList.Take(partsList.Count - 1);

        var parentFolder = await NavigateToFolder(serializedFolderBookmark, parentParts);
        await parentFolder.CreateFolderAsync(newFolderName);
    }

    public async Task<bool> DirectoryExists(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        try
        {
            await NavigateToFolder(serializedFolderBookmark, parts);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    public async Task<bool> FileExists(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        try
        {
            await NavigateToFile(serializedFolderBookmark, parts);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public async Task DirectoryDelete(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var folder = await NavigateToFolder(serializedFolderBookmark, parts);
        await folder.DeleteAsync();
    }

    public async Task FileDelete(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var file = await NavigateToFile(serializedFolderBookmark, parts);
        await file.DeleteAsync();
    }

    public async Task DirectoryMove(string serializedFolderBookmark, IEnumerable<string> fromParts, IEnumerable<string> toParts)
    {
        var fromFolder = await NavigateToFolder(serializedFolderBookmark, fromParts);

        var toPartsList = toParts.ToList();
        var newName = toPartsList[^1];
        var toParentParts = toPartsList.Take(toPartsList.Count - 1);
        var toParentFolder = await NavigateToFolder(serializedFolderBookmark, toParentParts);

        // Create destination folder with the new name
        var destFolder = await toParentFolder.CreateFolderAsync(newName) ?? throw new IOException($"Failed to create destination folder: {newName}");
        var items = await fromFolder
            .GetItemsAsync()
            .ToListAsync();

        foreach (var item in items)
            await item.MoveAsync(destFolder);
    }

    public async Task FileMove(string serializedFolderBookmark, IEnumerable<string> fromParts, IEnumerable<string> toParts)
    {
        var fromFile = await NavigateToFile(serializedFolderBookmark, fromParts);

        var toPartsList = toParts.ToList();
        var newName = toPartsList[^1];
        var toParentParts = toPartsList.Take(toPartsList.Count - 1);
        var toParentFolder = await NavigateToFolder(serializedFolderBookmark, toParentParts);

        // Create destination file with the new name
        var destFile = await toParentFolder.CreateFileAsync(newName) ?? throw new IOException($"Failed to create destination file: {newName}");

        // Copy content from source to destination
        await using (var sourceStream = await fromFile.OpenReadAsync())
        await using (var destStream = await destFile.OpenWriteAsync())
        {
            await sourceStream.CopyToAsync(destStream);
        }

        // Delete the source file
        await fromFile.DeleteAsync();
    }

    public async Task<Stream> FileCreate(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var partsList = parts.ToList();
        if (partsList.Count == 0)
            throw new ArgumentException("File path is empty");

        var fileName = partsList[^1];
        var folderParts = partsList.Take(partsList.Count - 1);

        var folder = await NavigateToFolder(serializedFolderBookmark, folderParts);
        var file = await folder.CreateFileAsync(fileName) ?? throw new IOException($"Failed to create file: {fileName}");

        return await file.OpenWriteAsync();
    }

    public async Task<Stream> FileOpenRead(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var file = await NavigateToFile(serializedFolderBookmark, parts);
        return await file.OpenReadAsync();
    }

    public async Task<DateTime> GetFileLastWriteTimeUtc(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var file = await NavigateToFile(serializedFolderBookmark, parts);
        var properties = await file.GetBasicPropertiesAsync();
        return properties.DateModified?.UtcDateTime ?? properties.DateCreated?.UtcDateTime ?? DateTime.UtcNow;
    }

    public async Task<long> GetFileLength(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var file = await NavigateToFile(serializedFolderBookmark, parts);
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

    public async Task<IEnumerable<FtpsServerFileSystemEntry>> DirectoryGetFileSystemEntries(string serializedFolderBookmark, IEnumerable<string> parts)
    {
        var folder = await NavigateToFolder(serializedFolderBookmark, parts);
        var result = new List<FtpsServerFileSystemEntry>();

        // Add current directory entry
        var folderProperties = await folder.GetBasicPropertiesAsync();
        result.Add(new FtpsServerFileSystemEntry(".", folderProperties.DateModified?.UtcDateTime ?? folderProperties.DateCreated?.UtcDateTime ?? DateTime.Now, 0, true));

        // Add parent directory entry
        var parent = await folder.GetParentAsync();
        if (parent != null)
        {
            var parentProperties = await parent.GetBasicPropertiesAsync();
            result.Add(new FtpsServerFileSystemEntry("..", parentProperties.DateModified?.UtcDateTime ?? parentProperties.DateCreated?.UtcDateTime ?? DateTime.Now, 0, true));
        }
        else
        {
            result.Add(new FtpsServerFileSystemEntry("..", folderProperties.DateModified?.UtcDateTime ?? folderProperties.DateCreated?.UtcDateTime ?? DateTime.Now, 0, true));
        }

        // Get all items in the folder
        var items = await folder.GetItemsAsync().ToListAsync();

        foreach (var item in items)
        {
            var properties = await item.GetBasicPropertiesAsync();
            var lastWriteTime = properties.DateModified?.UtcDateTime ?? properties.DateCreated?.UtcDateTime ?? DateTime.UtcNow;

            if (item is IStorageFile)
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
}
