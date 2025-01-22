using System.Xml.Linq;
using RFGM.Formats.Hashes;

namespace RFGM.Formats.Localization;

public static class LocalizationScraper
{
    public static Dictionary<uint, string> StringIdentifiers { get; private set; } = [];

    public static void GetIdentifiers(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.xtbl", SearchOption.AllDirectories)
            .Where(file => Path.GetFileName(file) != "credits.xtbl")) // Ignore - gives invalid character exception
        {
            try
            {
                var document = XDocument.Load(file);

                foreach (var element in document.Descendants().Where(e => !e.HasElements))
                {
                    var hash = Hash.HashVolitionCRCAlt(element.Value);

                    if (!StringIdentifiers.ContainsKey(hash))
                    {
                        StringIdentifiers.Add(hash, element.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {file}: {ex.Message}");
            }
        }
    }
}
