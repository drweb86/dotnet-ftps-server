using System.Text;
using System.Xml;

// =============================================================================
// WINGET LOCALE FILES
// =============================================================================
// Winget copy is not stored in .resx files. Those files are embedded in the
// Windows app, and the store text is not shown in the UI.
//
// For each fastlane locale folder:
//   fastlane/metadata/winget/{locale}/description.txt   Windows description
//   fastlane/metadata/android/{locale}/short_description.txt
// The folder name is the winget locale (en-US, zh-CN, pt-BR). en-US is the
// default locale. ResxSorter writes the YAML during winget submission; the
// generated files are not committed.
// =============================================================================

namespace Codice.SortResX
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            var sourceDir = Directory.GetCurrentDirectory();
            while (Path.GetFileName(sourceDir) != "sources")
                sourceDir = Directory.GetParent(sourceDir)!.FullName;

            var localizationDir = Path.Combine(sourceDir, "FtpsServerWindows", "Resources");

            var dictionary = new Dictionary<string, int>();
            var allResx = Directory
                .GetFiles(localizationDir, "*.resx")
                .OrderBy(x => x.Length);
            var mainFile = allResx.First();

            foreach (var resx in Directory
                .GetFiles(localizationDir, "*.resx")
                .OrderBy(x => x.Length))
            {
                Console.WriteLine($"Sorting {resx}");
                int countKeys = new FileProcessor(resx)
                    .Process();

                dictionary.Add(resx, countKeys);

                if (countKeys != dictionary[mainFile])
                {
                    var percent = (countKeys * 100.0) / (dictionary[mainFile] * 1.0);
                    if (percent < 95)
                    {
                        throw new Exception($"{Path.GetFileNameWithoutExtension(resx)} needs attention {percent}.");
                    }
                }
            }

            WingetLocaleGenerator.Generate(sourceDir);
        }
    }

    public class FileProcessor
    {
        public FileProcessor(string path)
        {
            mPath = path;
            mResourceNameList = new List<string>();
            mResourceNodes = new Dictionary<string, XmlNode>();
            mDoc = new XmlDocument();
            mDoc.Load(mPath);
        }

        public int Process()
        {
            ExtractResources("data/@name");
            var sortedNames = SortResourceList();
            WriteOrderedResources(sortedNames);
            return sortedNames.Count();
        }

        void ExtractResources(string query)
        {
            var nodesFileNames = Array.Empty<string>();

            foreach (XmlAttribute attribute in mDoc.DocumentElement!.SelectNodes(query)!)
            {
                var element = attribute.OwnerElement!;
                if (nodesFileNames.Contains(attribute.Value))
                {
                    foreach (XmlNode child in element.ChildNodes)
                    {
                        if (child.NodeType == XmlNodeType.Element)
                        {
                            var value = child.InnerText;

                            if (Path.GetInvalidPathChars().Any(x => value.Contains(x)) ||
                                Path.GetInvalidFileNameChars().Any(x => value.Contains(x)))
                                throw new Exception($"{attribute.Name} contains invalid path chars");
                        }
                    }
                }
                AddXmlNode(element, attribute);
                element.ParentNode!.RemoveChild(element);
            }
        }

        void AddXmlNode(XmlNode node, XmlAttribute attribute)
        {
            if (mResourceNodes.ContainsKey(attribute.Value.ToString()))
                return;

            mResourceNodes.Add(attribute.Value.ToString(), node);
            mResourceNameList.Add(attribute.Value.ToString());
        }

        string[] SortResourceList()
        {
            string[] names = new string[mResourceNameList.Count];

            for (int i = 0; i < mResourceNameList.Count; i++)
                names[i] = mResourceNameList[i];

            Array.Sort(names);
            return names;
        }

        void WriteOrderedResources(string[] names)
        {
            foreach (string key in names)
            {
                mDoc.DocumentElement!.AppendChild(mResourceNodes[key]);
            }

            mDoc.Save(mPath);
        }

        private List<string> mResourceNameList = null!;
        private Dictionary<string, XmlNode> mResourceNodes = null!;
        private XmlDocument mDoc = null!;
        private string mPath = null!;
    }

    public static class WingetLocaleGenerator
    {
        public static void Generate(string sourceDir)
        {
            var repoRoot = Directory.GetParent(sourceDir)!.FullName;
            var androidMetadataDir = Path.Combine(repoRoot, "fastlane", "metadata", "android");
            var wingetMetadataDir = Path.Combine(repoRoot, "fastlane", "metadata", "winget");
            var wingetPkgsDir = Path.Combine(sourceDir, "tools", "winget-pkgs");

            foreach (var localeDir in Directory.GetDirectories(wingetMetadataDir).OrderBy(x => x))
            {
                var wingetLocale = Path.GetFileName(localeDir);
                var descriptionPath = Path.Combine(localeDir, "description.txt");
                var shortPath = Path.Combine(androidMetadataDir, wingetLocale, "short_description.txt");

                if (!File.Exists(descriptionPath) || !File.Exists(shortPath))
                {
                    Console.WriteLine($"Missing winget texts for {wingetLocale}, skipping locale generation.");
                    continue;
                }

                var description = File.ReadAllText(descriptionPath).Replace("\r\n", "\n").TrimEnd('\n', '\r');
                var shortDescription = File.ReadAllText(shortPath).Replace("\r\n", "\n").Trim();
                var isDefaultLocale = wingetLocale.Equals("en-US", StringComparison.OrdinalIgnoreCase);
                var schemaType = isDefaultLocale ? "defaultLocale" : "locale";
                var manifestType = isDefaultLocale ? "defaultLocale" : "locale";
                var outputFileName = $"SiarheiKuchuk.FtpsServer.locale.{wingetLocale}.yaml";
                var outputPath = Path.Combine(wingetPkgsDir, outputFileName);

                using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(false));
                writer.WriteLine($"# yaml-language-server: $schema=https://aka.ms/winget-manifest.{schemaType}.1.12.0.schema.json");
                writer.WriteLine();
                writer.WriteLine("PackageIdentifier: SiarheiKuchuk.FtpsServer");
                writer.WriteLine("PackageVersion: APP_VERSION_STRING");
                writer.WriteLine($"PackageLocale: {wingetLocale}");
                writer.WriteLine("Publisher: Siarhei Kuchuk");
                writer.WriteLine("PublisherUrl: https://github.com/drweb86");
                writer.WriteLine("PublisherSupportUrl: https://github.com/drweb86/dotnet-ftps-server/issues");
                writer.WriteLine("Author: Siarhei Kuchuk");
                writer.WriteLine("PackageName: FTPS Server");
                writer.WriteLine("PackageUrl: https://github.com/drweb86/dotnet-ftps-server");
                writer.WriteLine("License: CC0-1.0");
                writer.WriteLine("LicenseUrl: https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE");
                writer.WriteLine("Copyright: 2025-CURRENT_YEAR Siarhei Kuchuk");
                writer.WriteLine("CopyrightUrl: https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE");
                writer.WriteLine($"ShortDescription: {YamlDoubleQuoted(shortDescription)}");
                writer.WriteLine("Description: |");
                foreach (var line in description.Split('\n'))
                {
                    var trimmedLine = line.TrimEnd('\r');
                    writer.WriteLine($"  {trimmedLine}");
                }
                if (isDefaultLocale)
                    writer.WriteLine("Moniker: ftpsserver");
                writer.WriteLine("Tags:");
                writer.WriteLine("- ftp");
                writer.WriteLine("- ftps");
                writer.WriteLine("- server");
                writer.WriteLine("- file-sharing");
                writer.WriteLine("ReleaseNotesUrl: https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/CHANGELOG.md");
                writer.WriteLine($"ManifestType: {manifestType}");
                writer.WriteLine("ManifestVersion: 1.12.0");

                Console.WriteLine($"Generated {outputPath}");
            }
        }

        // Double-quoted: plain YAML scalars fail on embedded ':' (e.g. trailing ':' in translations).
        private static string YamlDoubleQuoted(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(ch); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
