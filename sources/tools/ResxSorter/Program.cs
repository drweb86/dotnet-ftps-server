using System.Text;
using System.Xml;

// =============================================================================
// TECHNICAL KEYS IN RESX FILES (DO NOT TRANSLATE)
// =============================================================================
// This tool reads special "_Technical_*" keys from .resx files to generate
// winget manifest locale files. These keys should NOT be translated - they
// contain technical identifiers used by external systems.
//
// Available technical keys:
//
// 1. _Technical_WingetLocale
//    - Purpose: Winget package manager locale identifier
//    - Example values: "en-US", "de-DE", "zh-CN", "pt-BR"
//    - Used by: WingetLocaleGenerator to create locale.*.yaml files
//    - Required for: All languages that should have winget locale files
//
// Adding a new language:
//   1. Create Strings.{culture}.resx file in FtpsServerWindows/Resources
//   2. Add _Technical_WingetLocale with the appropriate locale code
//   3. Add Winget_ShortDescription and Winget_Description translations
//   4. Run ResxSorter to generate output files
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
            var localizationDir = Path.Combine(sourceDir, "FtpsServerWindows", "Resources");
            var wingetPkgsDir = Path.Combine(sourceDir, "tools", "winget-pkgs");

            var allResx = Directory.GetFiles(localizationDir, "*.resx")
                .OrderBy(x => x.Length);

            foreach (var resxPath in allResx)
            {
                var doc = new XmlDocument();
                doc.Load(resxPath);

                string? wingetLocale = null;
                string? shortDescription = null;
                string? description = null;

                foreach (XmlNode node in doc.SelectNodes("//data")!)
                {
                    var name = node.Attributes?["name"]?.Value;
                    if (name == "_Technical_WingetLocale")
                        wingetLocale = node.SelectSingleNode("value")?.InnerText;
                    else if (name == "Winget_ShortDescription")
                        shortDescription = node.SelectSingleNode("value")?.InnerText;
                    else if (name == "Winget_Description")
                        description = node.SelectSingleNode("value")?.InnerText;
                }

                if (string.IsNullOrWhiteSpace(wingetLocale))
                    continue;

                if (shortDescription == null || description == null)
                {
                    Console.WriteLine($"Missing Winget_ keys in {resxPath}, skipping locale generation.");
                    continue;
                }

                var culture = ExtractCulture(resxPath);
                var isDefaultLocale = culture == "";
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
                writer.WriteLine("PackageName: FtpsServer");
                writer.WriteLine("PackageUrl: https://github.com/drweb86/dotnet-ftps-server");
                writer.WriteLine("License: MIT");
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

        private static string ExtractCulture(string resxPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(resxPath);
            var dotIndex = fileName.IndexOf('.');
            return dotIndex >= 0 ? fileName[(dotIndex + 1)..] : "";
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
