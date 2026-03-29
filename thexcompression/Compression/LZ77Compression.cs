using System;
using System.Collections.Generic;
using System.Text;

namespace XCompressor.Compression
{
    public class LZ77Compression : ICompressionAlgorithm
    {
        private const int WindowSize = 4096;//sliding window
        private const int LookaheadBufferSize = 18;

        //encoding
        public string Compress(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            List<(int offset, int length, char next)> output = new List<(int, int, char)>();

            int pos = 0;
            while (pos < input.Length)
            {
                int matchLength = 0;
                int matchOffset = 0;

                int startWindow = Math.Max(0, pos - WindowSize);
                for (int i = startWindow; i < pos; i++)
                {
                    int length = 0;
                    while (length < LookaheadBufferSize &&
                           pos + length < input.Length &&
                           input[i + length] == input[pos + length])
                    {
                        length++;
                    }

                    if (length > matchLength)
                    {
                        matchLength = length;
                        matchOffset = pos - i;
                    }
                }

                char nextChar = (pos + matchLength < input.Length) ? input[pos + matchLength] : '\0';
                output.Add((matchOffset, matchLength, nextChar));
                pos += matchLength + 1;
            }

            //string converter
            StringBuilder sb = new StringBuilder();
            foreach (var t in output)
            {
                sb.Append($"{t.offset},{t.length},{t.next}|");
            }

            return sb.ToString();
        }

        public string Decompress(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            List<string> tokens = new List<string>(input.Split('|', StringSplitOptions.RemoveEmptyEntries));
            StringBuilder sb = new StringBuilder();

            foreach (var token in tokens)
            {
                var parts = token.Split(',');
                int offset = int.Parse(parts[0]);
                int length = int.Parse(parts[1]);
                char nextChar = parts[2][0];

                int startPos = sb.Length - offset;
                for (int i = 0; i < length; i++)
                {
                    sb.Append(sb[startPos + i]);
                }
                if (nextChar != '\0')
                    sb.Append(nextChar);
            }

            return sb.ToString();
        }

        //binary version
        public byte[] CompressBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            List<byte> output = new List<byte>();
            int pos = 0;

            while (pos < data.Length)
            {
                int matchLength = 0;
                int matchOffset = 0;

                int startWindow = Math.Max(0, pos - WindowSize);
                for (int i = startWindow; i < pos; i++)
                {
                    int length = 0;
                    while (length < LookaheadBufferSize &&
                           pos + length < data.Length &&
                           data[i + length] == data[pos + length])
                    {
                        length++;
                    }

                    if (length > matchLength)
                    {
                        matchLength = length;
                        matchOffset = pos - i;
                    }
                }

                byte nextByte = (byte)((pos + matchLength < data.Length) ? data[pos + matchLength] : 0);
                ////////////////////////////////////////////////////////
                ////
                output.Add((byte)(matchOffset >> 8));
                output.Add((byte)(matchOffset & 0xFF));
                output.Add((byte)matchLength);
                output.Add(nextByte);

                pos += matchLength + 1;
            }

            return output.ToArray();
        }

        public byte[] DecompressBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            List<byte> output = new List<byte>();
            int pos = 0;

            while (pos + 3 < data.Length)
            {
                int offset = (data[pos] << 8) | data[pos + 1];
                int length = data[pos + 2];
                byte nextByte = data[pos + 3];
                pos += 4;

                int startPos = output.Count - offset;
                for (int i = 0; i < length; i++)
                {
                    output.Add(output[startPos + i]);
                }
                if (nextByte != 0)
                    output.Add(nextByte);
            }

            return output.ToArray();
        }
    }
}