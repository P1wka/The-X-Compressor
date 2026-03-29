using System;
using System.Text;
using System.Collections.Generic;

namespace XCompressor.Compression
{
    public class RLECompression : ICompressionAlgorithm
    {
        //methods
        //string based
        public string Compress(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            StringBuilder sb = new StringBuilder();
            int count = 1;

            for (int i = 1; i <= input.Length; i++)
            {
                if (i < input.Length && input[i] == input[i - 1])
                {
                    count++;
                }
                else
                {
                    sb.Append(input[i - 1]);
                    if (count > 1)
                        sb.Append(count);
                    count = 1;
                }
            }

            return sb.ToString();
        }

        public string Decompress(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                StringBuilder num = new StringBuilder();

                // Sonraki karakterler sayi mı kontrol et
                while (i + 1 < input.Length && char.IsDigit(input[i + 1]))
                {
                    i++;
                    num.Append(input[i]);
                }

                int count = num.Length > 0 ? int.Parse(num.ToString()) : 1;
                sb.Append(new string(c, count));
            }

            return sb.ToString();
        }

        //methods
        //binary based
        public byte[] CompressBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            List<byte> compressed = new List<byte>();
            byte count = 1;

            for (int i = 1; i <= data.Length; i++)
            {
                if (i < data.Length && data[i] == data[i - 1] && count < 255)
                    count++;
                else
                {
                    compressed.Add(data[i - 1]);
                    compressed.Add(count);
                    count = 1;
                }
            }

            return compressed.ToArray();
        }

        public byte[] DecompressBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            List<byte> decompressed = new List<byte>();

            for (int i = 0; i < data.Length; i += 2)
            {
                byte value = data[i];
                byte count = data[i + 1];

                for (int j = 0; j < count; j++)
                    decompressed.Add(value);
            }

            return decompressed.ToArray();
        }
    }
}