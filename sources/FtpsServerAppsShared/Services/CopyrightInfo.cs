using System.Globalization;
using System.Reflection;

namespace FtpsServerAppsShared.Services;

public static class CopyrightInfo
{
    public static string Copyright { get; }

    public static Version Version { get; }

    static CopyrightInfo()
    {
        Version = Assembly
            .GetExecutingAssembly()
                .GetName()
            .Version ?? throw new InvalidProgramException("Failed to get assembly from !");

        Copyright = string.Format(CultureInfo.CurrentUICulture, "FTPS Server {0} : Copyright (c) 2025-{1} Siarhei Kuchuk", Version, DateTime.Now.Year);
    }
}
