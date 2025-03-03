using System.IO.Compression;
using System.Text.Json;

namespace ScheMigrator;

public class SchePackage
{
    private static readonly string PACKAGE_NAME = "SchePackage";
    private IList<string> Scripts { get; } = new List<string>();
    
    public SchePackage AddScript(string script)
    {
        Scripts.Add(script);
        return this;
    }

    public byte[] Package()
    {
        byte[] package = JsonSerializer.Serialize(Scripts).Select(c => (byte)c).ToArray();
        byte[] compressedBytes;

        using (var outStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
            {
                var fileInArchive = archive.CreateEntry(PACKAGE_NAME, CompressionLevel.Optimal);
                using (var entryStream = fileInArchive.Open())
                using (var fileToCompressStream = new MemoryStream(package))
                {
                    fileToCompressStream.CopyTo(entryStream);
                }
            }
            compressedBytes = outStream.ToArray();
        }
        return compressedBytes;
    }
}