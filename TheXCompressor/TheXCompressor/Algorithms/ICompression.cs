using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheXCompressor.Algorithms
{
    public interface ICompression
    {
        string Name { get; }

        string Compress(string input);

        string Decompress(string input);
    }
}
