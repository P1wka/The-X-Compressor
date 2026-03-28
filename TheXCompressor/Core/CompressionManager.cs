using System.Diagnostics;
using TheXCompressor.Algorithms;

namespace TheXCompressor.Core
{
    public class CompressionResult
    {
        public string Data { get; set; }
        public string Algorithm { get; set; }
        public int OriginalSize { get; set; }
        public int CompressedSize { get; set; }
        public double Ratio { get; set; }
        public long TimeMs { get; set; }
    }

    public class CompressionManager
    {
        private List<ICompression> _algorithms;

        public CompressionManager()
        {
            _algorithms = new List<ICompression>
            {
                new RLE(),
                new Huffman()
            };
        }

        public CompressionResult Compress(string data, string algorithmName)
        {
            var algo = _algorithms.FirstOrDefault(a => a.Name == algorithmName);

            if (algo == null)
                throw new Exception("Algorithm not found");

            return RunCompression(algo, data);
        }
        
        public CompressionResult CompressAuto(string data)
        {
            CompressionResult bestResult = null;

            foreach (var algo in _algorithms)
            {
                var result = RunCompression(algo, data);

                if (bestResult == null || result.CompressedSize < bestResult.CompressedSize)
                {
                    bestResult = result;
                }
            }

            return bestResult;
        }

        private CompressionResult RunCompression(ICompression algo, string data)
        {
            var sw = Stopwatch.StartNew();

            var compressed = algo.Compress(data);

            sw.Stop();

            return new CompressionResult
            {
                Data = compressed,
                Algorithm = algo.Name,
                OriginalSize = data.Length,
                CompressedSize = compressed.Length,
                Ratio = CalculateRatio(data.Length, compressed.Length),
                TimeMs = sw.ElapsedMilliseconds
            };
        }

        private double CalculateRatio(int original, int compressed)
        {
            return (1.0 - (double)compressed / original) * 100;
        }
    }
}
