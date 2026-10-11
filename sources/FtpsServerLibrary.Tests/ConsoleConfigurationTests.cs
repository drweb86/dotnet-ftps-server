using FtpsServerConsole;
using FtpsServerConsole.Resources;
using FtpsServerLibrary;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

namespace FtpsServerLibrary.Tests;

public class ConsoleConfigurationTests
{
    private static readonly string[] Cultures =
    [
        "am", "ar", "bn", "de", "es", "fa", "fr", "ha", "hi", "id", "ig", "it", "ja", "kk", "ko",
        "mr", "my", "ne", "om", "pa", "pcm", "pl", "ps", "pt-BR", "ru", "sw", "ta", "te", "th",
        "tr", "uk", "ur", "uz", "vi", "yo", "yue", "zh-Hans"
    ];

    [Fact]
    public void ResourceKeysMatchStringProperties()
    {
        var properties = typeof(Strings)
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();
        var keys = ReadKeys(Path.Combine(ResourcesDirectory(), "Strings.resx")).OrderBy(name => name).ToArray();
        Assert.Equal(properties, keys);
    }

    [Fact]
    public void EveryLanguageTranslatesEveryString()
    {
        var directory = ResourcesDirectory();
        var english = ReadMap(Path.Combine(directory, "Strings.resx"));
        foreach (var culture in Cultures)
        {
            var path = Path.Combine(directory, $"Strings.{culture}.resx");
            Assert.True(File.Exists(path), path);
            var translated = ReadMap(path);
            Assert.Equal(english.Keys.OrderBy(key => key), translated.Keys.OrderBy(key => key));
            foreach (var (key, value) in english)
            {
                Assert.Equal(Placeholders(value), Placeholders(translated[key]));
                if (key == "ConsoleHelp")
                {
                    Assert.Contains("--config", translated[key], StringComparison.Ordinal);
                    Assert.Contains("--user", translated[key], StringComparison.Ordinal);
                    Assert.Contains("--port", translated[key], StringComparison.Ordinal);
                }
            }

            foreach (var (key, value) in english)
            {
                if (key is "ConsoleCertSha256" or "ConsoleCertSha1")
                    continue;
                if (key == "ConsoleNo" && culture is "es" or "it")
                    continue;
                Assert.False(
                    string.Equals(value, translated[key], StringComparison.Ordinal),
                    $"{culture}:{key} is still English.");
            }
        }
    }

    [Fact]
    public void RussianResourcesLoadAtRuntime()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru");
            Assert.Equal("Выберите папку для общего доступа.", Strings.ConsoleFolderRequired);
            Assert.Contains("--config", Strings.ConsoleHelp, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void AcceptsTheSameLimitsAsTheDesktopScreen()
    {
        using var folder = new TempFolder();
        var config = Valid(folder.Path);
        Assert.Empty(ConfigurationValidator.Validate(config));

        config.ServerSettings.Port = 3000;
        Assert.Empty(ConfigurationValidator.Validate(config));

        config.ServerSettings.Port = 2120;
        Assert.Contains(ConfigurationValidator.Validate(config), Error(nameof(Strings.ConsoleInvalidPort)));

        config.ServerSettings.Port = 65536;
        Assert.Contains(ConfigurationValidator.Validate(config), Error(nameof(Strings.ConsoleInvalidPort)));

        config.ServerSettings.Port = 2121;
        config.ServerSettings.MaxConnections = 1;
        Assert.Contains(ConfigurationValidator.Validate(config), Error(nameof(Strings.ConsoleInvalidMaxConnections)));

        config.ServerSettings.MaxConnections = 101;
        Assert.Contains(ConfigurationValidator.Validate(config), Error(nameof(Strings.ConsoleInvalidMaxConnections)));
    }

    [Fact]
    public void RejectsAMissingFolderFromTheCommandLine()
    {
        var parsed = CommandLine.Parse(["--user", @"admin#secret#C:\no-such-ftps-folder-xyz#RW", "--port", "2121"]);
        Assert.Empty(parsed.Errors);
        var errors = ConfigurationValidator.Validate(parsed.Configuration);
        Assert.Contains(errors, Error(nameof(Strings.ConsoleFolderMissing)));
    }

    [Fact]
    public void RejectsAFileUsedAsAFolder()
    {
        var file = Path.GetTempFileName();
        try
        {
            var config = Valid(file);
            Assert.Contains(ConfigurationValidator.Validate(config), Error(nameof(Strings.ConsoleFolderIsFile)));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void RejectsIncompleteUsersAndCertificates()
    {
        using var folder = new TempFolder();
        var config = Valid(folder.Path);
        config.Users.Add(new FtpsServerUserAccount
        {
            Login = "Admin",
            Password = "other",
            Folder = folder.Path,
            Read = true,
            Write = false
        });
        config.Users[0].Password = " ";
        config.Users[0].Read = false;
        config.Users[0].Write = false;
        config.ServerSettings.Ip = "not-an-ip";
        config.ServerSettings.CertificatePath = folder.Path;
        config.ServerSettings.CertificateStoreSubject = "only-subject";

        var errors = ConfigurationValidator.Validate(config);
        Assert.Contains(errors, Error(nameof(Strings.ConsoleDuplicateUser)));
        Assert.Contains(errors, Error(nameof(Strings.ConsolePasswordRequired)));
        Assert.Contains(errors, Error(nameof(Strings.ConsolePermissionRequired)));
        Assert.Contains(errors, Error(nameof(Strings.ConsoleInvalidIp)));
        Assert.Contains(errors, Error(nameof(Strings.ConsoleCertIsFolder)));
        Assert.Contains(errors, Error(nameof(Strings.ConsoleCertStoreIncomplete)));
    }

    [Fact]
    public void RejectsAMalformedUserArgument()
    {
        var parsed = CommandLine.Parse(["--user", "admin#secret#folder", "--port", "nope", "--certstorename", "Nope"]);
        Assert.Contains(parsed.Errors, Error(nameof(Strings.ConsoleUserArgument)));
        Assert.Contains(parsed.Errors, Error(nameof(Strings.ConsoleInvalidNumber)));
        Assert.Contains(parsed.Errors, Error(nameof(Strings.ConsoleInvalidStoreName)));
    }

    private static FtpsServerConfiguration Valid(string folder) => new()
    {
        ServerSettings =
        {
            Ip = "0.0.0.0",
            Port = 2121,
            MaxConnections = 10
        },
        Users =
        [
            new FtpsServerUserAccount
            {
                Login = "admin",
                Password = "secret",
                Folder = folder,
                Read = true,
                Write = true
            }
        ]
    };

    private static Predicate<string> Error(string key)
    {
        var text = key switch
        {
            nameof(Strings.ConsoleInvalidPort) => Strings.ConsoleInvalidPort,
            nameof(Strings.ConsoleInvalidMaxConnections) => Strings.ConsoleInvalidMaxConnections,
            nameof(Strings.ConsoleFolderMissing) => Strings.ConsoleFolderMissing,
            nameof(Strings.ConsoleFolderIsFile) => Strings.ConsoleFolderIsFile,
            nameof(Strings.ConsoleDuplicateUser) => Strings.ConsoleDuplicateUser,
            nameof(Strings.ConsolePasswordRequired) => Strings.ConsolePasswordRequired,
            nameof(Strings.ConsolePermissionRequired) => Strings.ConsolePermissionRequired,
            nameof(Strings.ConsoleInvalidIp) => Strings.ConsoleInvalidIp,
            nameof(Strings.ConsoleCertIsFolder) => Strings.ConsoleCertIsFolder,
            nameof(Strings.ConsoleCertStoreIncomplete) => Strings.ConsoleCertStoreIncomplete,
            nameof(Strings.ConsoleUserArgument) => Strings.ConsoleUserArgument,
            nameof(Strings.ConsoleInvalidNumber) => Strings.ConsoleInvalidNumber,
            nameof(Strings.ConsoleInvalidStoreName) => Strings.ConsoleInvalidStoreName,
            _ => throw new ArgumentOutOfRangeException(nameof(key))
        };
        var token = text.Split('{')[0].Trim();
        return error => error.Contains(token, StringComparison.CurrentCulture);
    }

    private static List<string> Placeholders(string value) =>
        System.Text.RegularExpressions.Regex.Matches(value, @"\{(\d+)\}").Select(match => match.Value).ToList();

    private static string ResourcesDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var nested = Path.Combine(dir.FullName, "sources", "FtpsServerConsole", "Resources");
            if (Directory.Exists(nested))
                return nested;
            var direct = Path.Combine(dir.FullName, "FtpsServerConsole", "Resources");
            if (Directory.Exists(direct))
                return direct;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Console resources were not found.");
    }

    private static IReadOnlyDictionary<string, string> ReadMap(string path)
    {
        var document = XDocument.Load(path);
        return document.Root!.Elements("data")
            .ToDictionary(
                element => element.Attribute("name")!.Value,
                element => element.Element("value")!.Value);
    }

    private static IEnumerable<string> ReadKeys(string path) => ReadMap(path).Keys;

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = Directory.CreateTempSubdirectory("ftps-console-").FullName;
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
