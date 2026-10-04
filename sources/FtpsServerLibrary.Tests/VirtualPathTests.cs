namespace FtpsServerLibrary.Tests;

public class VirtualPathTests
{
    [Theory]
    [InlineData("..", "/")]
    [InlineData("../..", "/")]
    [InlineData("a/..", "/")]
    [InlineData("a/../b", "/b")]
    [InlineData("a/b/../../c", "/c")]
    [InlineData("a/../../c", "/c")]
    [InlineData(".", "/")]
    [InlineData("a/./b", "/a/b")]
    [InlineData("a\\..\\b", "/b")]
    [InlineData("..\\..\\windows", "/windows")]
    public void ParentSegmentsCannotClimbAboveTheRoot(string path, string expected)
    {
        var virtualPath = new FtpsServerVirtualPath(path);
        Assert.Equal(expected, virtualPath.ToFtpsPath());
        Assert.DoesNotContain(virtualPath.Segments, segment => segment is "." or "..");
    }

    [Fact]
    public void GoUpFromRootStaysAtRoot()
    {
        Assert.Equal("/", new FtpsServerVirtualPath().GoUp().ToFtpsPath());
    }

    [Fact]
    public void GoUpRemovesOnlyTheLastSegment()
    {
        Assert.Equal("/a", new FtpsServerVirtualPath("a/b").GoUp().ToFtpsPath());
    }

    [Fact]
    public void RelativeAppendStillCollapsesParents()
    {
        Assert.Equal("/b", new FtpsServerVirtualPath("secret").Append("../b").ToFtpsPath());
        Assert.Equal("/etc/passwd", new FtpsServerVirtualPath("secret").Append("../../etc/passwd").ToFtpsPath());
    }

    [Fact]
    public void AbsoluteAppendReplacesThePathAndDropsParents()
    {
        Assert.Equal("/public", new FtpsServerVirtualPath("secret/docs").Append("/public").ToFtpsPath());
        Assert.Equal("/etc", new FtpsServerVirtualPath("secret").Append("/../../etc").ToFtpsPath());
    }

    [Theory]
    [InlineData("a\0")]
    [InlineData("\0")]
    [InlineData("a/\0b")]
    [InlineData("ok/bad\0")]
    public void NullByteIsRejected(string path)
    {
        Assert.Throws<InvalidOperationException>(() => new FtpsServerVirtualPath(path));
    }

    [Theory]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData(" both ")]
    [InlineData("ok/ trailing")]
    [InlineData("ok/leading ")]
    public void EdgeSpaceIsRejected(string path)
    {
        Assert.Throws<InvalidOperationException>(() => new FtpsServerVirtualPath(path));
    }

    [Theory]
    [InlineData("file name", "/file name")]
    [InlineData("a/b c/d", "/a/b c/d")]
    public void InternalSpaceIsKept(string path, string expected)
    {
        Assert.Equal(expected, new FtpsServerVirtualPath(path).ToFtpsPath());
    }

    [Theory]
    [InlineData("con")]
    [InlineData("CON")]
    [InlineData("Con.txt")]
    [InlineData("nul.")]
    [InlineData("aux.txt.bak")]
    [InlineData("prn")]
    [InlineData("com1")]
    [InlineData("COM9.dat")]
    [InlineData("lpt1")]
    [InlineData("lpt9.txt")]
    [InlineData("folder/con")]
    [InlineData("folder\\LPT3.log")]
    public void ReservedDeviceNameIsRejected(string path)
    {
        Assert.Throws<InvalidOperationException>(() => new FtpsServerVirtualPath(path));
    }

    [Theory]
    [InlineData("file.txt", "/file.txt")]
    [InlineData("console", "/console")]
    [InlineData("com10", "/com10")]
    [InlineData("com0", "/com0")]
    [InlineData("aux1", "/aux1")]
    [InlineData("file.con", "/file.con")]
    [InlineData("...", "/...")]
    public void OrdinaryNameIsKept(string path, string expected)
    {
        Assert.Equal(expected, new FtpsServerVirtualPath(path).ToFtpsPath());
    }
}
