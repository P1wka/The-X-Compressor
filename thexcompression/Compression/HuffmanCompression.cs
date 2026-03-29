using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace XCompressor.Compression
{
    public class HuffmanNode : IComparable<HuffmanNode>
    {
        public char Symbol;
        public int Frequency;
        public HuffmanNode Left, Right;
        public bool IsLeaf => Left == null && Right == null;
        public int CompareTo(HuffmanNode other) => Frequency - other.Frequency;
    }

    public class HuffmanCompression
    {
        private HuffmanNode root;
        private Dictionary<char, string> codes;

        // ---------------- Compress ----------------
        public byte[] Compress(string input)
        {
            if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();

            //calculate frequency
            var freq = input.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

            //create huffman tree
            var pq = new SortedSet<HuffmanNode>(Comparer<HuffmanNode>.Create((a, b) =>
            {
                int cmp = a.Frequency.CompareTo(b.Frequency);
                if (cmp == 0) return a.Symbol.CompareTo(b.Symbol);
                return cmp;
            }));

            foreach (var kvp in freq)
                pq.Add(new HuffmanNode { Symbol = kvp.Key, Frequency = kvp.Value });

            while (pq.Count > 1)
            {
                var left = pq.Min; pq.Remove(left);
                var right = pq.Min; pq.Remove(right);

                pq.Add(new HuffmanNode
                {
                    Symbol = '\0',
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                });
            }

            root = pq.Min;

            //create code table
            codes = new Dictionary<char, string>();
            BuildCodes(root, "");

            //create bit list
            var bitString = new StringBuilder();
            foreach (var c in input)
                bitString.Append(codes[c]);

            //turn bit to byte 
            int byteCount = (bitString.Length + 7) / 8;
            byte[] compressed = new byte[byteCount];
            for (int i = 0; i < bitString.Length; i++)
            {
                if (bitString[i] == '1')
                    compressed[i / 8] |= (byte)(1 << (7 - (i % 8)));
            }

            return compressed;
        }

        // ---------------- Decompress ----------------
        public string Decompress(byte[] data, int originalLength)
        {
            if (data == null || data.Length == 0 || root == null) return string.Empty;

            var sb = new StringBuilder();
            var node = root;
            int totalBits = data.Length * 8;

            for (int i = 0; i < totalBits; i++)
            {
                int bIndex = i / 8;
                int bitIndex = 7 - (i % 8);
                int bit = (data[bIndex] >> bitIndex) & 1;

                node = bit == 0 ? node.Left : node.Right;

                if (node.IsLeaf)
                {
                    sb.Append(node.Symbol);
                    node = root;

                    //stop when it was original size
                    if (sb.Length == originalLength)
                        break;
                }
            }

            return sb.ToString();
        }

        private void BuildCodes(HuffmanNode node, string code)
        {
            if (node.IsLeaf)
            {
                codes[node.Symbol] = code == "" ? "0" : code;
                return;
            }
            BuildCodes(node.Left, code + "0");
            BuildCodes(node.Right, code + "1");
        }
    }

    public class HuffmanBinaryNode : IComparable<HuffmanBinaryNode>
    {
        public byte Symbol;
        public int Frequency;
        public HuffmanBinaryNode Left, Right;

        public int CompareTo(HuffmanBinaryNode other) => Frequency - other.Frequency;

        public bool IsLeaf => Left == null && Right == null;
    }

    public class HuffmanCompressionBinary
    {
        //build codes from tree
        private Dictionary<byte, string> BuildCodes(HuffmanBinaryNode node)
        {
            var codes = new Dictionary<byte, string>();
            void Build(HuffmanBinaryNode n, string code)
            {
                if (n == null) return;
                if (n.IsLeaf)
                {
                    codes[n.Symbol] = code == "" ? "0" : code;//single leaf case
                    return;
                }
                Build(n.Left, code + "0");
                Build(n.Right, code + "1");
            }
            Build(node, "");
            return codes;
        }

        //build tree from a frequency table
        private HuffmanBinaryNode BuildTree(Dictionary<byte, int> freq)
        {
            var pq = new SortedSet<HuffmanBinaryNode>(Comparer<HuffmanBinaryNode>.Create((a, b) =>
            {
                int cmp = a.Frequency.CompareTo(b.Frequency);
                if (cmp != 0) return cmp;
                int sc = a.Symbol.CompareTo(b.Symbol);
                if (sc != 0) return sc;
                //tie-breaker to avoid equality for different instances
                return RuntimeHelpers.GetHashCode(a).CompareTo(RuntimeHelpers.GetHashCode(b));
            }));

            foreach (var kvp in freq)
                pq.Add(new HuffmanBinaryNode { Symbol = kvp.Key, Frequency = kvp.Value });

            if (pq.Count == 0) return null;

            while (pq.Count > 1)
            {
                var left = pq.Min; pq.Remove(left);
                var right = pq.Min; pq.Remove(right);

                var parent = new HuffmanBinaryNode
                {
                    Symbol = 0,
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };
                pq.Add(parent);
            }

            return pq.Min;
        }

        //build tree from raw data
        private HuffmanBinaryNode BuildTree(byte[] data)
        {
            var freq = new Dictionary<byte, int>();
            foreach (var b in data)
                freq[b] = freq.ContainsKey(b) ? freq[b] + 1 : 1;
            return BuildTree(freq);
        }

        // Compress with header containing original length and frequency table so decompression can rebuild tree
        // Format:
        // [4 bytes] original length (int32, little-endian)
        // [4 bytes] unique count (int32)
        // for each unique:
        //   [1 byte] symbol
        //   [4 bytes] frequency (int32)
        // [payload bytes] raw compressed bits (padded to full byte)
        public byte[] CompressBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            //frequency
            var freq = new Dictionary<byte, int>();
            foreach (var b in data)
                freq[b] = freq.ContainsKey(b) ? freq[b] + 1 : 1;

            var root = BuildTree(freq);
            var codes = BuildCodes(root);

            //compute total bits
            long totalBitsLong = 0;
            foreach (var kvp in freq)
            {
                if (codes.TryGetValue(kvp.Key, out var code))
                    totalBitsLong += (long)kvp.Value * code.Length;
            }
            if (totalBitsLong > int.MaxValue) throw new InvalidOperationException("Data too large to compress in this implementation.");
            int totalBits = (int)totalBitsLong;
            int payloadBytes = (totalBits + 7) / 8;
            byte[] payload = new byte[payloadBytes];

            //write bits
            int bitPos = 0;
            for (int i = 0; i < data.Length; i++)
            {
                var code = codes[data[i]];
                for (int j = 0; j < code.Length; j++)
                {
                    if (code[j] == '1')
                    {
                        int byteIndex = bitPos / 8;
                        int bitIndex = 7 - (bitPos % 8);
                        payload[byteIndex] |= (byte)(1 << bitIndex);
                    }
                    bitPos++;
                }
            }

            //build header
            var headerList = new List<byte>();
            headerList.AddRange(BitConverter.GetBytes(data.Length));
            headerList.AddRange(BitConverter.GetBytes(freq.Count));
            foreach (var kvp in freq)
            {
                headerList.Add(kvp.Key);
                headerList.AddRange(BitConverter.GetBytes(kvp.Value));
            }

            //combine header + payload
            var result = new byte[headerList.Count + payload.Length];
            Buffer.BlockCopy(headerList.ToArray(), 0, result, 0, headerList.Count);
            if (payload.Length > 0)
                Buffer.BlockCopy(payload, 0, result, headerList.Count, payload.Length);

            return result;
        }

        //decodes payload bits
        public byte[] DecompressBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();
            int offset = 0;
            if (data.Length < 8) throw new ArgumentException("Invalid compressed data format.");

            int originalLength = BitConverter.ToInt32(data, offset); offset += 4;
            int uniqueCount = BitConverter.ToInt32(data, offset); offset += 4;

            var freq = new Dictionary<byte, int>();
            for (int i = 0; i < uniqueCount; i++)
            {
                if (offset + 5 > data.Length) throw new ArgumentException("Invalid compressed data format.");
                byte symbol = data[offset];
                int f = BitConverter.ToInt32(data, offset + 1);
                freq[symbol] = f;
                offset += 5;
            }

            var root = BuildTree(freq);
            if (root == null || originalLength == 0) return Array.Empty<byte>();

            //if single unique symbol repeat it
            if (root.IsLeaf)
            {
                var single = new byte[originalLength];
                for (int i = 0; i < originalLength; i++) single[i] = root.Symbol;
                return single;
            }

            var result = new List<byte>(originalLength);
            var node = root;

            int totalPayloadBits = (data.Length - offset) * 8;
            int bitsRead = 0;

            for (int i = 0; i < totalPayloadBits && result.Count < originalLength; i++)
            {
                int bIndex = offset + (i / 8);
                int bitIndex = 7 - (i % 8);
                int bit = (data[bIndex] >> bitIndex) & 1;

                node = bit == 0 ? node.Left : node.Right;

                if (node.IsLeaf)
                {
                    result.Add(node.Symbol);
                    node = root;
                }
            }

            return result.ToArray();
        }
    }
}