using System.IO.Compression;
using System.Text.Json;
using System.Text;

namespace DataBlocks.Migrations;

public class ScheModelPackage
{
    private static readonly string PACKAGE_NAME = "SchePackage";
    private Package ScriptsPackage { get; init; }
    
    public IEnumerable<string> Scripts => ScriptsPackage.Scripts;

    public ScheModelPackage(SqlImplementation implementation)
    {
        ScriptsPackage = new Package { Implementation = implementation };
    }
    
    private ScheModelPackage(Package package)
    {
        ScriptsPackage = package;
    }
    
    public ScheModelPackage AddScript(string script)
    {
        ScriptsPackage.Scripts.Add(script);
        return this;
    }

    public byte[] MakePackage()
    {
        byte[] package = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ScriptsPackage));

        using var outStream = new MemoryStream();
        using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
        {
            var fileInArchive = archive.CreateEntry(PACKAGE_NAME, CompressionLevel.Optimal);
            using (var entryStream = fileInArchive.Open())
            using (var fileToCompressStream = new MemoryStream(package))
            {
                fileToCompressStream.CopyTo(entryStream);
            }
        }

        return outStream.ToArray();
    }

    public static ScheModelPackage LoadPackage(byte[] contents)
    {
        byte[] decompressedBytes;
        ScheModelPackage package;

        using var outStream = new MemoryStream();
        using (var inStream = new MemoryStream(contents))
        {
            var fileOutArchive = new ZipArchive(inStream, ZipArchiveMode.Read);
            using (var entryStream = fileOutArchive.GetEntry(PACKAGE_NAME)?.Open())
            {
                if (entryStream == null)
                {
                    throw new Exception($"Package contents malformed: expected inner zip file {PACKAGE_NAME}");
                }

                entryStream.CopyTo(outStream);
            }
        }

        decompressedBytes = outStream.ToArray();

        Package? packageContents = JsonSerializer.Deserialize<Package>(Encoding.UTF8.GetString(decompressedBytes));
        if (packageContents == null)
        {
            throw new Exception("Package content is null");
        }

        return new ScheModelPackage(packageContents);
    }
    
    private class Package
    {
        public required SqlImplementation Implementation { get; init; }
        public IList<string> Scripts { get; init; } = new List<string>();
    }
}