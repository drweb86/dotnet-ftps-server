using System.Diagnostics;

namespace FtpsServerLibrary.Tests;

public sealed class SymlinkEscapeTests : IDisposable
{
    private readonly string _root;
    private readonly string _share;
    private readonly string _outside;
    private readonly FtpsServerFileSystemProvider _provider = new();

    public SymlinkEscapeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ftps-sec-" + Guid.NewGuid().ToString("N"));
        _share = Path.Combine(_root, "share");
        _outside = Path.Combine(_root, "outside");
        Directory.CreateDirectory(_share);
        Directory.CreateDirectory(_outside);
        File.WriteAllText(Path.Combine(_share, "inside.txt"), "inside");
        File.WriteAllText(Path.Combine(_outside, "secret.txt"), "secret");
    }

    public void Dispose()
    {
        DeleteWithoutFollowingLinks(_root);
    }

    [Fact]
    public async Task FileInsideTheShareCanBeRead()
    {
        Assert.True(await _provider.FileExists(_share, ["inside.txt"]));
        await using var stream = await _provider.FileOpenRead(_share, ["inside.txt"]);
        using var reader = new StreamReader(stream);
        Assert.Equal("inside", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task MissingNameInsideTheShareIsNotAnEscape()
    {
        Assert.False(await _provider.FileExists(_share, ["missing.txt"]));
    }

    [Fact]
    public async Task ParentNavigationThatStaysInsideTheShareIsAllowed()
    {
        var sub = Path.Combine(_share, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "nested.txt"), "nested");

        Assert.True(await _provider.FileExists(_share, ["sub", "..", "inside.txt"]));
    }

    [Fact]
    public async Task ParentSegmentCannotReachOutsideTheShare()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _provider.FileExists(_share, ["..", "outside", "secret.txt"]));
    }

    [Fact]
    public async Task RootedPathIsRejected()
    {
        var rooted = Path.Combine(_outside, "secret.txt");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _provider.FileExists(_share, [rooted]));
    }

    [Fact]
    public async Task LinkToOutsideIsRejected()
    {
        CreateDirectoryLink(Path.Combine(_share, "escape"), _outside);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _provider.FileExists(_share, ["escape", "secret.txt"]));

        // A file symlink needs the symlink privilege. The directory link above is the check that always runs.
        var fileLink = Path.Combine(_share, "leak.txt");
        if (!TryCreateFileLink(fileLink, Path.Combine(_outside, "secret.txt")))
            return;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _provider.FileExists(_share, ["leak.txt"]));
    }

    [Fact]
    public async Task DirectoryLinkToOutsideIsOmittedFromTheListing()
    {
        CreateDirectoryLink(Path.Combine(_share, "escape"), _outside);

        var entries = await _provider.DirectoryGetFileSystemEntries(_share, []);
        var names = entries.Select(entry => entry.FileName).ToList();

        Assert.Contains("inside.txt", names);
        Assert.DoesNotContain("escape", names);
    }

    [Fact]
    public async Task DirectoryLinkThatStaysInsideTheShareIsFollowed()
    {
        var docs = Path.Combine(_share, "docs");
        Directory.CreateDirectory(docs);
        File.WriteAllText(Path.Combine(docs, "file.txt"), "file");
        CreateDirectoryLink(Path.Combine(_share, "alias"), docs);

        Assert.True(await _provider.DirectoryExists(_share, ["alias"]));
        Assert.True(await _provider.FileExists(_share, ["alias", "file.txt"]));
    }

    [Fact]
    public async Task LinkChainThatLeavesTheShareIsRejected()
    {
        var hop2 = Path.Combine(_share, "hop2");
        CreateDirectoryLink(hop2, _outside);
        CreateDirectoryLink(Path.Combine(_share, "hop1"), hop2);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _provider.FileExists(_share, ["hop1", "secret.txt"]));
    }

    [Fact]
    public async Task CaseVariantSiblingIsRejectedWhenTheDirectoriesDiffer()
    {
        var lower = Path.Combine(_root, "ftp");
        var upper = Path.Combine(_root, "FTP");
        Directory.CreateDirectory(lower);
        Directory.CreateDirectory(upper);
        var probeName = "probe-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(Path.Combine(upper, probeName), "probe");
        if (File.Exists(Path.Combine(lower, probeName)))
        {
            var expected = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            Assert.Equal(expected, FtpsServerFileSystemProvider.PathComparison);
            return;
        }

        File.WriteAllText(Path.Combine(upper, "secret.txt"), "secret");
        CreateDirectoryLink(Path.Combine(lower, "escape"), upper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _provider.FileExists(lower, ["escape", "secret.txt"]));
    }

    private static void CreateDirectoryLink(string linkPath, string targetPath)
    {
        var target = Path.GetFullPath(targetPath);
        try
        {
            Directory.CreateSymbolicLink(linkPath, target);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            if (!OperatingSystem.IsWindows())
                throw;
        }

        var start = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{linkPath}\" \"{target}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start cmd.exe.");
        if (!process.WaitForExit(10000))
            throw new InvalidOperationException("mklink did not finish.");
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
            throw new InvalidOperationException(error);
        }
    }

    private static bool TryCreateFileLink(string linkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, Path.GetFullPath(targetPath));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static void DeleteWithoutFollowingLinks(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                if ((attributes & FileAttributes.Directory) != 0)
                    Directory.Delete(entry);
                else
                    File.Delete(entry);
                continue;
            }

            if ((attributes & FileAttributes.Directory) != 0)
                DeleteWithoutFollowingLinks(entry);
            else
                File.Delete(entry);
        }

        Directory.Delete(directory);
    }
}
