using System.IO;
using System.IO.Compression;
using XCompressor.Compression;

public class DeflateCompression : ICompressionAlgorithm
{
    //compressing
    public byte[] CompressBytes(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var ds = new DeflateStream(ms, CompressionLevel.Optimal))
        {
            ds.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    //decompressing text-binary
    public byte[] DecompressBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var ds = new DeflateStream(ms, CompressionMode.Decompress);
        using var outMs = new MemoryStream();
        ds.CopyTo(outMs);
        return outMs.ToArray();
    }

    ////////////////////////////////////////////////////////////////////////////////
    public string Compress(string input) => Convert.ToBase64String(CompressBytes(System.Text.Encoding.UTF8.GetBytes(input)));
    public string Decompress(string input) => System.Text.Encoding.UTF8.GetString(DecompressBytes(Convert.FromBase64String(input)));
}