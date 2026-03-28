using System;
using System.Text;

namespace TheXCompressor.Algorithms
{
    public class LZ77 : ICompression
    {
        public string Name => "LZ77";

        private int windowSize = 20;

        public string Compress(string input)
        {
            if(string.IsNullOrEmpty(input)) return "";

            var output = new StringBuilder();
            int pos = 0;

            while(pos < input.Length)
            {
                int matchLength = 0;
                int matchDistance = 0;

                int start = Math.Max(0, pos - windowSize);

                for(int i = start; i < pos; i++)
                {
                    int length = 0;
                    while(pos + length < input.Length && input[i + length] == input[pos + length])
                    {
                        length++;
                        if(i + length >= pos)
                        {
                            break;
                        }
                    }

                    if(length > matchLength)
                    {
                        matchLength = length;
                        matchDistance = pos - i;
                    }
                }

                if (matchLength > 1)
                {
                    output.Append($"({matchDistance},{matchLength})");
                    pos += matchLength;
                }
                else
                {
                    output.Append(input[pos]);
                    pos++;
                }
            }

            return output.ToString();
        }

        public string Decompress(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var output = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '(')
                {
                    int j = i + 1;
                    while (input[j] != ')') j++;
                    var token = input.Substring(i + 1, j - i - 1);
                    var parts = token.Split(',');
                    int distance = int.Parse(parts[0]);
                    int length = int.Parse(parts[1]);

                    int start = output.Length - distance;
                    for (int k = 0; k < length; k++)
                        output.Append(output[start + k]);

                    i = j;
                }
                else
                {
                    output.Append(input[i]);
                }
            }

            return output.ToString();
        }
    }
}