using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace TheXCompressor.Algorithms
{
    public class HuffmanNode
    {
        public char? Character { get; set; }
        public int Frequency { get; set; }
        public HuffmanNode Left { get; set; }
        public HuffmanNode Right { get; set; }

        public bool IsLeaf => Left == null && Right == null;
    }
    public class Huffman : ICompression
    {
        public string Name => "Huffman";



        public string Compress(string input)
        {
            if(string.IsNullOrEmpty(input))
            {
                return "";
            }

            var frequency = input.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

            var nodes = new List<HuffmanNode>();

            foreach(var kv in frequency)
            {
                nodes.Add(new HuffmanNode
                {
                    Character = kv.Key,
                    Frequency = kv.Value
                });
            }

            while(nodes.Count > 1)
            {
                nodes = nodes.OrderBy(n => n.Frequency).ToList();

                var left = nodes[0];
                var right = nodes[1];

                var parent = new HuffmanNode
                {
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };

                nodes.Remove(left);
                nodes.Remove(right);
                nodes.Add(parent);
            }

            var root = nodes[0];

            // generate codes
            var codes = new Dictionary<char, string>();
            BuildCodes(root, "", codes);

            // encode
            var encoded = new StringBuilder();
            foreach (char c in input)
                encoded.Append(codes[c]);

            // basic solution add header
            var header = string.Join("|", frequency.Select(kv => $"{kv.Key}:{kv.Value}"));
            
            return header + "\n" + encoded.ToString();
            //return input;
        }

        public string Decompress(string input)
        {
            return input;
        }

        private void BuildCodes(HuffmanNode node, string code, Dictionary<char, string> codes)
        {
            if (node == null) return;

            if (node.IsLeaf)
            {
                codes[node.Character.Value] = code;
            }

            BuildCodes(node.Left, code + "0", codes);
            BuildCodes(node.Right, code + "1", codes);
        }

        //public static Node BuildTree()
    }
}
