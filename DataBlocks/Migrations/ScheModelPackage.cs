using System.IO.Compression;
using System.Text.Json;
using System.Text;
using SharpCompress.Common;

namespace DataBlocks.Migrations;

public class ScheModelPackage
{
    private static readonly string PACKAGE_NAME = "SchePackage";
    public IList<string> Scripts { get; private init; } = new List<string>();
    
    public ScheModelPackage AddScript(string script)
    {
        Scripts.Add(script);
        return this;
    }

    public byte[] Package()
    {
        byte[] package = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Scripts));

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

        IList<string>? packageContents = JsonSerializer.Deserialize<IList<string>>(Encoding.UTF8.GetString(decompressedBytes));
        if (packageContents == null)
        {
            throw new Exception("Package content is null");
        } 
        
        return new ScheModelPackage()
        {
            Scripts = packageContents 
        };
    }
}