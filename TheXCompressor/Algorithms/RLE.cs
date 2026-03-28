using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheXCompressor.Algorithms
{
    public class RLE : ICompression
    {
        public string Name => "RLE";

        public string Compress(string input)
        {
            if(string.IsNullOrEmpty(input))
            {
                return "";
            }

            var result = new System.Text.StringBuilder();
            int count = 1;

            for (int i = 1; i < input.Length; i++)
            {
                if (input[i] == input[i - 1])
                {
                    count++;
                }
                else
                {
                    AppendEncoded(result, input[i - 1], count);
                    count = 1;
                }
            }

            AppendEncoded(result, input[^1], count);

            return result.ToString();
        }

        public string Decompress(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            var result = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];

               // if there is a # read numbers
                if (i + 1 < input.Length && input[i + 1] == '#')
                {
                    int j = i + 2;
                    int count = 0;

                    while (j < input.Length && char.IsDigit(input[j]))
                    {
                        count = count * 10 + (input[j] - '0');
                        j++;
                    }

                    for (int k = 0; k < count; k++)
                        result.Append(current);

                    i = j - 1;
                }
                else
                {
                    result.Append(current);
                }
            }

            return result.ToString();
        }


        private void AppendEncoded(StringBuilder sb, char c, int count)
        {
            if (count >= 3)
            {
                sb.Append(c);
                sb.Append('#'); //data fix
                sb.Append(count);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    sb.Append(c);
            }
        }
    }
}
