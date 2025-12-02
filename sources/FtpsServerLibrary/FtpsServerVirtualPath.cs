using System.Text;

namespace FtpsServerLibrary;

class FtpsServerVirtualPath
{
    private readonly List<string> _segments;
    private const char _delimiter = '/';

    public bool IsAbsolute { get; private set; }

    public FtpsServerVirtualPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        _segments = [];
        ParsePath(path);
    }

    /// <summary>
    /// Initializes a new instance with predefined segments.
    /// </summary>
    private FtpsServerVirtualPath(List<string> segments, bool isAbsolute)
    {
        _segments = [.. segments];
        IsAbsolute = isAbsolute;
    }

    /// <summary>
    /// Parses the input path and normalizes it.
    /// </summary>
    private void ParsePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            IsAbsolute = false;
            return;
        }

        IsAbsolute = path.StartsWith(_delimiter.ToString());

        // Split by delimiter and process segments
        string[] parts = path.Split([_delimiter], StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            // Security check: reject null bytes
            if (part.Contains('\0'))
                throw new InvalidOperationException("Path contains null bytes which are not allowed.");

            // Handle current directory (.)
            if (part == ".")
                continue;

            // Handle parent directory (..)
            if (part == "..")
            {
                if (_segments.Count > 0)
                {
                    _segments.RemoveAt(_segments.Count - 1);
                }
                else if (!IsAbsolute)
                {
                    // For relative paths, we can add .. segments
                    _segments.Add(part);
                }
                // For absolute paths, .. at root is ignored
                continue;
            }

            // Add normal segment
            _segments.Add(part);
        }
    }

    /// <summary>
    /// Returns the full virtual path as a string.
    /// </summary>
    public override string ToString()
    {
        if (_segments.Count == 0)
        {
            return IsAbsolute ? "/" : ".";
        }

        StringBuilder sb = new();

        if (IsAbsolute)
            sb.Append(_delimiter);

        sb.Append(string.Join(_delimiter.ToString(), _segments));

        return sb.ToString();
    }

    /// <summary>
    /// Appends a path segment or path to the current virtual path.
    /// </summary>
    /// <param name="path">The path to append.</param>
    /// <returns>A new VirtualPath instance with the appended path.</returns>
    public FtpsServerVirtualPath Append(string path)
    {
        if (string.IsNullOrEmpty(path))
            return new FtpsServerVirtualPath(_segments, IsAbsolute);

        // If appending an absolute path, it replaces the current path
        if (path.StartsWith(_delimiter.ToString()))
        {
            return new FtpsServerVirtualPath(path);
        }

        // Create a combined path
        string combinedPath = ToString();
        if (!combinedPath.EndsWith(_delimiter.ToString()) && combinedPath != ".")
            combinedPath += _delimiter;
        combinedPath += path;

        return new FtpsServerVirtualPath(combinedPath);
    }

    /// <summary>
    /// Appends a path segment or path to the current virtual path.
    /// </summary>
    /// <param name="virtualPath">The VirtualPath to append.</param>
    /// <returns>A new VirtualPath instance with the appended path.</returns>
    public FtpsServerVirtualPath Append(FtpsServerVirtualPath? virtualPath)
    {
        ArgumentNullException.ThrowIfNull(virtualPath);

        return Append(virtualPath.ToString());
    }

    /// <summary>
    /// Moves up one directory level.
    /// </summary>
    /// <returns>A new VirtualPath instance representing the parent directory.</returns>
    public FtpsServerVirtualPath GoUp()
    {
        return Append("..");
    }

    /// <summary>
    /// Gets the real filesystem path by combining with a base path.
    /// Includes security checks to ensure the result doesn't escape the base path.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <returns>The full real path.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the path attempts to escape the base path.</exception>
    public string GetRealPath(string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
            throw new ArgumentNullException(nameof(basePath));

        // Normalize the base path
        string normalizedBase = Path.GetFullPath(basePath);

        // Ensure base path ends with directory separator
        if (!normalizedBase.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !normalizedBase.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            normalizedBase += Path.DirectorySeparatorChar;
        }

        // Convert virtual path to system-appropriate path
        string virtualPathStr = ToString();

        // Remove leading / for absolute virtual paths when combining with base
        if (IsAbsolute && virtualPathStr.StartsWith('/'))
            virtualPathStr = virtualPathStr[1..];

        // Replace / with system directory separator
        virtualPathStr = virtualPathStr.Replace('/', Path.DirectorySeparatorChar);

        // Combine paths
        if (Path.IsPathRooted(virtualPathStr))
            throw new UnauthorizedAccessException($"{virtualPathStr} path rooted path supplied");
        string combinedPath = Path.Combine(normalizedBase, virtualPathStr);

        // Get the full normalized path
        string fullPath = Path.GetFullPath(combinedPath);
        if (basePath.Contains('\\'))
            fullPath = fullPath.Replace('/', '\\');
        else
            fullPath = fullPath.Replace('\\', '/');


        // Security check: ensure the result is within the base path
        if (!fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Access denied. The path '{ToString()}' attempts to escape the base directory '{basePath}'.");
        }

        return fullPath;
    }

    /// <summary>
    /// Checks if this virtual path would escape the base path.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <returns>True if the path is safe; otherwise false.</returns>
    public bool IsSafeWithinBase(string basePath)
    {
        try
        {
            GetRealPath(basePath);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
