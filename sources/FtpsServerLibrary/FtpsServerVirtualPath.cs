using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FtpsServerLibrary;

[DebuggerDisplay("{string.Join(\",\", _segments)}")]
class FtpsServerVirtualPath(params IEnumerable<string> pathsOrSegments)
{
    private readonly List<string> _segments = 
        ParsePath(pathsOrSegments.SelectMany(x => x.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)))
        .Where(IsAllowed)
#pragma warning disable IDE0305 // Simplify collection initialization
        .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization

    private static readonly char[] _delimiters = ['/', '\\'];
    public IEnumerable<string> Segments => _segments;
    private static readonly List<string> _forbiddenNames = [".", "..", "con", "prn", "aux", "nul", "com1", "com2", 
        "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"];

    private static bool IsAllowed(string segmentPart)
    {
        if (segmentPart.Contains('\0'))
            throw new InvalidOperationException("Path segment cannot contain null bytes.");
        if (segmentPart.StartsWith(' '))
            throw new InvalidOperationException("Path segment cannot contain space in the beginning.");
        if (segmentPart.EndsWith(' '))
            throw new InvalidOperationException("Path segment cannot contain space in the end.");
        var lower = segmentPart.ToLower();
        if (_forbiddenNames.Contains(lower))
            throw new InvalidOperationException($"Path segment '{lower}' is forbidden.");
        return true;
    }

    private static List<string> ParsePath(params IEnumerable<string> parts)
    {
        var segments = new List<string>();

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
                if (segments.Count > 0)
                {
                    segments.RemoveAt(segments.Count - 1);
                }
                // For absolute paths, .. at root is ignored
                continue;
            }

            // Add normal segment
            segments.Add(part);
        }
        return segments;
    }

    public FtpsServerVirtualPath Append(string path)
    {
        if (path.StartsWith('/'))
            return new FtpsServerVirtualPath(path);
        return new FtpsServerVirtualPath(Segments.ToList().Append(path));
    }

    public FtpsServerVirtualPath Append(IEnumerable<string> paths)
    {
        var result = Segments.ToList();
        result.AddRange(paths);
        return Append(result);
    }

    public FtpsServerVirtualPath GoUp()
    {
        return Append("..");
    }

    public string ToFtpsPath()
    {
        return "/" + string.Join("/", Segments);
    }
}
