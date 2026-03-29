using System;
using System.Collections.Generic;
using System.Text;

namespace XCompressor.Compression
{
    public class LZWCompression : ICompressionAlgorithm
    {
        private const int MaxDictionarySize = 4096;

        // ================= TEXT =================

        public string Compress(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var dictionary = new Dictionary<string, int>();
            for (int i = 0; i < 256; i++)
                dictionary[((char)i).ToString()] = i;

            int dictSize = 256;
            string w = "";
            List<int> result = new List<int>();

            foreach (char c in input)
            {
                string wc = w + c;
                if (dictionary.ContainsKey(wc))
                {
                    w = wc;
                }
                else
                {
                    result.Add(dictionary[w]);

                    if (dictSize < MaxDictionarySize)
                        dictionary[wc] = dictSize++;

                    w = c.ToString();
                }
            }

            if (!string.IsNullOrEmpty(w))
                result.Add(dictionary[w]);

            return string.Join(",", result);
        }

        public string Decompress(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var codes = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var dictionary = new Dictionary<int, string>();

            for (int i = 0; i < 256; i++)
                dictionary[i] = ((char)i).ToString();

            int dictSize = 256;

            string w = dictionary[int.Parse(codes[0])];
            StringBuilder result = new StringBuilder(w);

            for (int i = 1; i < codes.Length; i++)
            {
                int k = int.Parse(codes[i]);

                string entry;
                if (dictionary.ContainsKey(k))
                    entry = dictionary[k];
                else if (k == dictSize)
                    entry = w + w[0];
                else
                    throw new Exception("Invalid LZW code!");

                result.Append(entry);

                if (dictSize < MaxDictionarySize)
                    dictionary[dictSize++] = w + entry[0];

                w = entry;
            }

            return result.ToString();
        }

        // ================= BINARY =================

        public byte[] CompressBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            var dict = new Dictionary<string, int>();
            for (int i = 0; i < 256; i++)
                dict[((char)i).ToString()] = i;

            int dictSize = 256;
            string w = "";
            List<int> codes = new List<int>();

            foreach (byte b in data)
            {
                char c = (char)b;
                string wc = w + c;

                if (dict.ContainsKey(wc))
                {
                    w = wc;
                }
                else
                {
                    codes.Add(dict[w]);

                    if (dictSize < MaxDictionarySize)
                        dict[wc] = dictSize++;

                    w = c.ToString();
                }
            }

            if (!string.IsNullOrEmpty(w))
                codes.Add(dict[w]);

            //convert int list to byte list
            List<byte> output = new List<byte>();
            foreach (int code in codes)
            {
                output.Add((byte)(code >> 8));
                output.Add((byte)(code & 0xFF));
            }

            return output.ToArray();
        }

        public byte[] DecompressBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            List<int> codes = new List<int>();
            for (int i = 0; i < data.Length; i += 2)
            {
                int code = (data[i] << 8) | data[i + 1];
                codes.Add(code);
            }

            var dict = new Dictionary<int, string>();
            for (int i = 0; i < 256; i++)
                dict[i] = ((char)i).ToString();

            int dictSize = 256;

            string w = dict[codes[0]];
            StringBuilder result = new StringBuilder(w);

            for (int i = 1; i < codes.Count; i++)
            {
                int k = codes[i];

                string entry;
                if (dict.ContainsKey(k))
                    entry = dict[k];
                else if (k == dictSize)
                    entry = w + w[0];
                else
                    throw new Exception("Invalid LZW binary code!");

                result.Append(entry);

                if (dictSize < MaxDictionarySize)
                    dict[dictSize++] = w + entry[0];

                w = entry;
            }

            //convert string list to byte list
            byte[] output = new byte[result.Length];
            for (int i = 0; i < result.Length; i++)
                output[i] = (byte)result[i];

            return output;
        }
    }
}