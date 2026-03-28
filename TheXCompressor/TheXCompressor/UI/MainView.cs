using NStack;
using Terminal.Gui;
using TheXCompressor.Core;

namespace TheXCompressor.UI
{
    public class MainView
    {
        private CompressionManager _manager;

        public MainView()
        {
            //_manager = new CompressionManager();
        }
        public void Setup()
        {
            var top = Application.Top;

            var win = new Window("The X Compressor | v0.02")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(win);

            ////////////////////////
            ///


            var fileLabel = new Label("File Path:")
            {
                X = 2,
                Y = 2
            };

            var fileInput = new TextField("")
            {
                X = 2,
                Y = 3,
                Width = 50
            };

            var algoLabel = new Label("Algorithm:")
            {
                X = 2,
                Y = 5
            };

            var algoSelector = new RadioGroup(new ustring[]
            {
                "Auto",
                "RLE",
                "Huffman"
            })
            {
                X = 2,
                Y = 6
            };

            var compressBtn = new Button("Compress")
            {
                X = 2,
                Y = 10
            };

            var decompressBtn = new Button("Decompress")
            {
                X = 15,
                Y = 10
            };

            var outputLabel = new Label("Output:")
            {
                X = 2,
                Y = 12
            };

            var outputBox = new TextView()
            {
                X = 2,
                Y = 13,
                Width = 60,
                Height = 10,
                ReadOnly = true
            };

            _manager = new CompressionManager();

            compressBtn.Clicked += () =>
            {
                string file = fileInput.Text.ToString();

                string path = Path.Combine(Environment.CurrentDirectory, file);

                if (!File.Exists(path))
                {
                    outputBox.Text = $"File not found! {path}";
                    return;
                }

                string data = File.ReadAllText(path);

                CompressionResult result;

                if (algoSelector.SelectedItem == 0) // Auto
                {
                    result = _manager.CompressAuto(data);
                }
                else
                {
                    string algoName = algoSelector.SelectedItem switch
                    {
                        1 => "RLE",
                        2 => "Huffman",
                        _ => "RLE"
                    };

                    result = _manager.Compress(data, algoName);
                }

                outputBox.Text =
                    $"Algorithm: {result.Algorithm}\n" +
                    $"Original: {result.OriginalSize}\n" +
                    $"Compressed: {result.CompressedSize}\n" +
                    $"Ratio: %{result.Ratio:F2}\n" +
                    $"Time: {result.TimeMs} ms";
            };

            win.Add(
                fileLabel,
                fileInput,
                algoLabel,
                algoSelector,
                compressBtn,
                decompressBtn,
                outputLabel,
                outputBox
            );

            //win.ColorScheme = new ColorScheme()
        }
    }
}
