namespace FtpsServerLibrary.Tests;

public class PathContainmentTests
{
    [Theory]
    [InlineData("/srv/ftp/", "/srv/ftp/file", true)]
    [InlineData("/srv/ftp/", "/srv/ftp", true)]
    [InlineData("/srv/ftp/", "/srv/ftp/", true)]
    [InlineData("/srv/ftp/", "/srv/ftp-other/file", false)]
    [InlineData("/srv/ftp/", "/srv/ftps/file", false)]
    [InlineData("/srv/ftp/", "/etc/passwd", false)]
    [InlineData("/srv/ftp/", "/srv/ftp/Secret", true)]
    public void TrailingSeparatorKeepsSiblingPrefixesOutside(string basePath, string fullPath, bool inside)
    {
        Assert.Equal(inside, FtpsServerFileSystemProvider.IsInsideBase(basePath, fullPath, StringComparison.Ordinal));
        Assert.Equal(inside, FtpsServerFileSystemProvider.IsInsideBase(basePath, fullPath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CaseVariantOfTheShareIsInsideOnlyWhenComparisonIgnoresCase()
    {
        const string share = "/srv/ftp/";
        const string sibling = "/srv/FTP/secret";

        Assert.False(FtpsServerFileSystemProvider.IsInsideBase(share, sibling, StringComparison.Ordinal));
        Assert.True(FtpsServerFileSystemProvider.IsInsideBase(share, sibling, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WindowsPathsUseTheSameContainmentRule()
    {
        const string share = @"C:\share\";

        Assert.True(FtpsServerFileSystemProvider.IsInsideBase(share, @"C:\share\file", StringComparison.Ordinal));
        Assert.False(FtpsServerFileSystemProvider.IsInsideBase(share, @"C:\Share\secret", StringComparison.Ordinal));
        Assert.True(FtpsServerFileSystemProvider.IsInsideBase(share, @"C:\Share\secret", StringComparison.OrdinalIgnoreCase));
        Assert.False(FtpsServerFileSystemProvider.IsInsideBase(share, @"C:\share2\secret", StringComparison.OrdinalIgnoreCase));
        Assert.False(FtpsServerFileSystemProvider.IsInsideBase(share, @"D:\share\file", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PlatformComparisonIsIgnoreCaseOnlyOnWindows()
    {
        var expected = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        Assert.Equal(expected, FtpsServerFileSystemProvider.PathComparison);
    }
}
