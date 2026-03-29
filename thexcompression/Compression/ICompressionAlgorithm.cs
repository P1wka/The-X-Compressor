using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCompressor.Compression
{
    public interface ICompressionAlgorithm
    {
        string Compress(string input);
        string Decompress(string input);
    }
}
