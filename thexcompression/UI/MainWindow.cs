using System;
using System.IO;
using System.Text;
using Terminal.Gui;
using XCompressor.Compression;
using XCompressor.Utils;
using NStack;

namespace XCompressor.UI
{
    public class MainWindow : Window
    {
        private readonly TextField fileNameField;
        private readonly TextView output;
        private readonly RadioGroup algorithmSelector;
        private readonly RLECompression rleCompressor;
        private readonly HuffmanCompression huffmanCompressor;
        private readonly HuffmanCompressionBinary huffmanBinaryCompressor;
        private readonly LZ77Compression lz77Compressor;
        private readonly LZWCompression lzwCompressor;
        private readonly DeflateCompression deflateCompressor;
        private readonly string examplesDir;

        private enum Algorithm
        {
            RLE = 0,
            HuffmanText = 1,
            HuffmanBinary = 2,
            LZ77 = 3,
            LZW = 4,
            Deflate = 5,
            AUTO = 6,
        }

        public MainWindow()
            : base()
        {
            Width = Dim.Fill();
            Height = Dim.Fill();

            rleCompressor = new RLECompression();
            huffmanCompressor = new HuffmanCompression();
            huffmanBinaryCompressor = new HuffmanCompressionBinary();
            lz77Compressor = new LZ77Compression();
            lzwCompressor = new LZWCompression();
            deflateCompressor = new DeflateCompression();

            examplesDir = Path.Combine(AppContext.BaseDirectory, "examples");
            if (!Directory.Exists(examplesDir))
            {
                Directory.CreateDirectory(examplesDir);
            }

            // title panel
            var titlePanel = new FrameView("🌟 XCompressor | v1.2 🌟")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 3,
                ColorScheme = Colors.TopLevel
            };
            Add(titlePanel);

            // file name input
            Add(new Label("File Name (e.g., test.txt)") { X = 2, Y = 4 });
            fileNameField = new TextField(string.Empty)
            {
                X = 2,
                Y = 5,
                Width = Dim.Fill() - 4
            };
            Add(fileNameField);

            //algorithm selection
            Add(new Label("Algorithm:") { X = 2, Y = 6 });
            algorithmSelector = new RadioGroup(new ustring[] { "RLE", "Huffman Text", "Huffman Binary", "LZ77", "LZW", "Deflate", "Auto" })
            {
                X = 2,
                Y = 7,
                Width = 40,
                //keep the radio group height constrained so it does not overlap the output panel.
                Height = 6
            };
            Add(algorithmSelector);

            //output panel
            var outputFrame = new FrameView("Output")
            {
                //place output just below the algorithm selector so it is visible
                X = 2,
                Y = Pos.Bottom(algorithmSelector) + 1,
                Width = Dim.Fill() - 4,
                Height = Dim.Fill() - 6
            };
            output = new TextView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = true
            };
            outputFrame.Add(output);
            Add(outputFrame);

            // buttons
            var compressBtn = new Button("Compress") { X = 2, Y = Pos.Bottom(outputFrame) + 2 };
            var decompressBtn = new Button("Decompress") { X = 14, Y = Pos.Bottom(outputFrame) + 2 };
            var exitBtn = new Button("Exit") { X = 30, Y = Pos.Bottom(outputFrame) + 2 };

            compressBtn.Clicked += CompressFile;
            decompressBtn.Clicked += DecompressFile;
            exitBtn.Clicked += () => Application.RequestStop();

            Add(compressBtn, decompressBtn, exitBtn);
        }

        private Algorithm GetSelectedAlgorithm()
        {
            var idx = algorithmSelector.SelectedItem;
            return Enum.IsDefined(typeof(Algorithm), idx) ? (Algorithm)idx : Algorithm.RLE;
        }

        private static string SafeFileName(string input) => Path.GetFileName(input ?? string.Empty);

        private static bool IsTextExtension(string ext) =>
            string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase);

        private static bool IsCompressedTextExtension(string ext) =>
            ext is ".rle" or ".huff" or ".lz77" or ".lzw" || IsTextExtension(ext);

        private static bool IsBinaryCompressedExtension(string ext) =>
            ext is ".rlebin" or ".huffbin" or ".lz77bin" or ".lzwbin";

        private void CompressFile()
        {
            // guard against possible null Text in the TextField
            var inputText = fileNameField.Text?.ToString() ?? string.Empty;
            var requestedName = SafeFileName(inputText.Trim());
            if (string.IsNullOrEmpty(requestedName))
            {
                output.Text = "❌ Please enter a file name!";
                return;
            }

            var path = Path.Combine(examplesDir, requestedName);
            if (!File.Exists(path))
            {
                output.Text = $"❌ File not found: {requestedName}";
                return;
            }

            try
            {
                var originalSize = new FileInfo(path).Length;
                var ext = Path.GetExtension(path).ToLowerInvariant();
                var algorithm = GetSelectedAlgorithm();
                var isText = IsTextExtension(ext);
                string resultFile;
                long resultSize;

                if (algorithm == Algorithm.AUTO)
                {
                    // Auto: try available compressors and choose by highest compression ratio
                    if (originalSize == 0)
                    {
                        // create an empty compressed file
                        resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".auto");
                        FileHelper.WriteFile(resultFile, Array.Empty<byte>());
                        resultSize = 0;
                        output.Text =
                            $"✅ File: {requestedName}\n" +
                            $"File Type: {(isText ? "📄 Text" : "🖼️ Binary")}\n" +
                            $"Algorithm: Auto (empty input)\n" +
                            $"Original Size: {originalSize} bytes\n" +
                            $"Compressed Size: {resultSize} bytes\n" +
                            $"Compression Ratio: 0.00%\n" +
                            $"Status: Success!\n" +
                            $"Saved File: {Path.GetFileName(resultFile)}";
                        return;
                    }

                    if (isText)
                    {
                        var content = FileHelper.ReadFile(path);
                        // candidates: RLE (string), HuffmanText (bytes), LZ77 (string), LZW (string), Deflate (bytes/string)
                        var rleBytes = Encoding.UTF8.GetBytes(rleCompressor.Compress(content));
                        var huffBytes = huffmanCompressor.Compress(content);
                        var lz77Bytes = Encoding.UTF8.GetBytes(lz77Compressor.Compress(content));
                        var lzwBytes = Encoding.UTF8.GetBytes(lzwCompressor.Compress(content));

                        // Ensure deflate result is treated as bytes regardless of its concrete return type
                        object deflateResult = deflateCompressor.Compress(content);
                        byte[] deflateBytes = deflateResult switch
                        {
                            byte[] b => b,
                            string s => Encoding.UTF8.GetBytes(s),
                            _ => Array.Empty<byte>()
                        };

                        // compute compression reduction ratio (%) = (original - compressed)/original * 100
                        double rleRatio = originalSize > 0 ? 100.0 * (originalSize - rleBytes.Length) / originalSize : 0.0;
                        double huffRatio = originalSize > 0 ? 100.0 * (originalSize - huffBytes.Length) / originalSize : 0.0;
                        double lz77Ratio = originalSize > 0 ? 100.0 * (originalSize - lz77Bytes.Length) / originalSize : 0.0;
                        double lzwRatio = originalSize > 0 ? 100.0 * (originalSize - lzwBytes.Length) / originalSize : 0.0;
                        double deflateRatio = originalSize > 0 ? 100.0 * (originalSize - deflateBytes.Length) / originalSize : 0.0;

                        var best = (algo: "RLE", extn: ".rle", data: rleBytes, ratio: rleRatio);
                        if (huffRatio > best.ratio) best = ("HuffmanText", ".huff", huffBytes, huffRatio);
                        if (lz77Ratio > best.ratio) best = ("LZ77", ".lz77", lz77Bytes, lz77Ratio);
                        if (lzwRatio > best.ratio) best = ("LZW", ".lzw", lzwBytes, lzwRatio);
                        if (deflateRatio > best.ratio) best = ("Deflate", ".deflate", deflateBytes, deflateRatio);

                        resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + best.extn);
                        FileHelper.WriteFile(resultFile, best.data);
                        resultSize = new FileInfo(resultFile).Length;

                        var compressionRatio = originalSize > 0 ? 100.0 * (originalSize - resultSize) / originalSize : 0.0;
                        output.Text =
                            $"✅ File: {requestedName}\n" +
                            $"File Type: 📄 Text\n" +
                            $"Algorithm: Auto -> {best.algo} (highest reduction)\n" +
                            $"Original Size: {originalSize} bytes\n" +
                            $"Compressed Size: {resultSize} bytes\n" +
                            $"Compression Ratio: {compressionRatio:F2}%\n" +
                            $"Status: Success!\n" +
                            $"Saved File: {Path.GetFileName(resultFile)}";
                        return;
                    }
                    else
                    {
                        // binary input: try RLE bytes, HuffmanBinary, LZ77 bytes, LZW bytes, Deflate bytes
                        var bytes = FileHelper.ReadFileBytes(path);
                        var rleBytes = rleCompressor.CompressBytes(bytes);
                        var huffBinBytes = huffmanBinaryCompressor.CompressBytes(bytes);
                        var lz77Bytes = lz77Compressor.CompressBytes(bytes);
                        var lzwBytes = lzwCompressor.CompressBytes(bytes);
                        var deflateBytes = deflateCompressor.CompressBytes(bytes);

                        double rleRatio = originalSize > 0 ? 100.0 * (originalSize - rleBytes.Length) / originalSize : 0.0;
                        double huffBinRatio = originalSize > 0 ? 100.0 * (originalSize - huffBinBytes.Length) / originalSize : 0.0;
                        double lz77Ratio = originalSize > 0 ? 100.0 * (originalSize - lz77Bytes.Length) / originalSize : 0.0;
                        double lzwRatio = originalSize > 0 ? 100.0 * (originalSize - lzwBytes.Length) / originalSize : 0.0;
                        double deflateRatio = originalSize > 0 ? 100.0 * (originalSize - deflateBytes.Length) / originalSize : 0.0;

                        var best = (algo: "RLE", extn: ".rlebin", data: rleBytes, ratio: rleRatio);
                        if (huffBinRatio > best.ratio) best = ("HuffmanBinary", ".huffbin", huffBinBytes, huffBinRatio);
                        if (lz77Ratio > best.ratio) best = ("LZ77", ".lz77bin", lz77Bytes, lz77Ratio);
                        if (lzwRatio > best.ratio) best = ("LZW", ".lzwbin", lzwBytes, lzwRatio);
                        if (deflateRatio > best.ratio) best = ("Deflate", ".deflatebin", deflateBytes, deflateRatio);

                        resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + best.extn);
                        FileHelper.WriteFile(resultFile, best.data);
                        resultSize = new FileInfo(resultFile).Length;

                        var compressionRatio = originalSize > 0 ? 100.0 * (originalSize - resultSize) / originalSize : 0.0;
                        output.Text =
                            $"✅ File: {requestedName}\n" +
                            $"File Type: 🖼️ Binary\n" +
                            $"Algorithm: Auto -> {best.algo} (highest reduction)\n" +
                            $"Original Size: {originalSize} bytes\n" +
                            $"Compressed Size: {resultSize} bytes\n" +
                            $"Compression Ratio: {compressionRatio:F2}%\n" +
                            $"Status: Success!\n" +
                            $"Saved File: {Path.GetFileName(resultFile)}";
                        return;
                    }
                }

                switch (algorithm)
                {
                    case Algorithm.RLE:
                        if (isText)
                        {
                            var content = FileHelper.ReadFile(path);
                            var compressed = rleCompressor.Compress(content);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".rle");
                            FileHelper.WriteFile(resultFile, compressed);
                        }
                        else
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var compressedBytes = rleCompressor.CompressBytes(bytes);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".rlebin");
                            FileHelper.WriteFile(resultFile, compressedBytes);
                        }
                        break;

                    case Algorithm.HuffmanText:
                        if (!isText)
                        {
                            output.Text = "❌ Huffman Text only supports text files!";
                            return;
                        }
                        {
                            var content = FileHelper.ReadFile(path);
                            var compressedBytes = huffmanCompressor.Compress(content);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".huff");
                            FileHelper.WriteFile(resultFile, compressedBytes);
                        }
                        break;

                    case Algorithm.HuffmanBinary:
                        {
                            // If the input already uses the .huffbin extension, skip to avoid double compression
                            if (ext == ".huffbin")
                            {
                                output.Text = "❗ File already has the .huffbin extension. Please choose a different file to compress.";
                                return;
                            }

                            var fileInfo = new FileInfo(path);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".huffbin");

                            if (fileInfo.Length == 0)
                            {
                                //nothing to compress
                                //or create an empty compressed file.
                                FileHelper.WriteFile(resultFile, Array.Empty<byte>());
                            }
                            else
                            {
                                //once read and once compress
                                var bytes = FileHelper.ReadFileBytes(path);
                                var compressedBytes = huffmanBinaryCompressor.CompressBytes(bytes);
                                FileHelper.WriteFile(resultFile, compressedBytes);
                            }
                        }
                        break;

                    case Algorithm.LZ77:
                        if (isText)
                        {
                            var content = FileHelper.ReadFile(path);
                            var compressed = lz77Compressor.Compress(content);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".lz77");
                            FileHelper.WriteFile(resultFile, compressed);
                        }
                        else
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var compressedBytes = lz77Compressor.CompressBytes(bytes);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".lz77bin");
                            FileHelper.WriteFile(resultFile, compressedBytes);
                        }
                        break;

                    case Algorithm.LZW:
                        if (isText)
                        {
                            var content = FileHelper.ReadFile(path);
                            var compressed = lzwCompressor.Compress(content);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".lzw");
                            FileHelper.WriteFile(resultFile, compressed);
                        }
                        else
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var compressedBytes = lzwCompressor.CompressBytes(bytes);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".lzwbin");
                            FileHelper.WriteFile(resultFile, compressedBytes);
                        }

                        break;

                    case Algorithm.Deflate:
                        {
                            var content = FileHelper.ReadFile(path);
                            var compressedBytes = deflateCompressor.Compress(content);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".deflate");
                            FileHelper.WriteFile(resultFile, compressedBytes);
                        }
                        break;

                    default:
                        throw new InvalidOperationException("Unknown algorithm selected.");
                }

                resultSize = new FileInfo(resultFile).Length;

                var compressionRatioFinal = originalSize > 0 ? 100.0 * resultSize / originalSize : 0.0;
                var fileType = isText ? "📄 Text" : "🖼️ Binary";

                output.Text =
                    $"✅ File: {requestedName}\n" +
                    $"File Type: {fileType}\n" +
                    $"Algorithm: {algorithm}\n" +
                    $"Original Size: {originalSize} bytes\n" +
                    $"Compressed Size: {resultSize} bytes\n" +
                    $"Compression Ratio: {compressionRatioFinal:F2}%\n" +
                    $"Status: Success!\n" +
                    $"Saved File: {Path.GetFileName(resultFile)}";
            }
            catch (Exception ex)
            {
                output.Text = $"❌ Error: {ex.Message}";
            }
        }

        private void DecompressFile()
        {
            // guard against possible null Text in the TextField
            var inputText = fileNameField.Text?.ToString() ?? string.Empty;
            var requestedName = SafeFileName(inputText.Trim());

            if (string.IsNullOrEmpty(requestedName))
            {
                output.Text = "❌ Please enter a file name!";
                return;
            }

            var path = Path.Combine(examplesDir, requestedName);
            if (!File.Exists(path))
            {
                output.Text = $"❌ File not found: {requestedName}";
                return;
            }

            try
            {
                var originalSize = new FileInfo(path).Length;
                var ext = Path.GetExtension(path).ToLowerInvariant();
                var algorithm = GetSelectedAlgorithm();
                string resultFile;
                long resultSize;

                switch (algorithm)
                {
                    case Algorithm.RLE:
                        if (IsCompressedTextExtension(ext))
                        {
                            var content = FileHelper.ReadFile(path);
                            var decompressed = rleCompressor.Decompress(content);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.txt");
                            FileHelper.WriteFile(resultFile, decompressed);
                        }
                        else
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var decompressedBytes = rleCompressor.DecompressBytes(bytes);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.bin");
                            FileHelper.WriteFile(resultFile, decompressedBytes);
                        }
                        break;

                    case Algorithm.HuffmanText:
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var decompressed = huffmanCompressor.Decompress(bytes, (int)bytes.Length);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.txt");
                            FileHelper.WriteFile(resultFile, decompressed);
                        }
                        break;

                    case Algorithm.HuffmanBinary:
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var decompressedBytes = huffmanBinaryCompressor.DecompressBytes(bytes);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.bin");
                            FileHelper.WriteFile(resultFile, decompressedBytes);
                        }
                        break;

                    case Algorithm.LZ77:
                        if (IsCompressedTextExtension(ext))
                        {
                            var content = FileHelper.ReadFile(path);
                            var decompressed = lz77Compressor.Decompress(content);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.txt");
                            FileHelper.WriteFile(resultFile, decompressed);
                        }
                        else
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var decompressedBytes = lz77Compressor.DecompressBytes(bytes);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.bin");
                            FileHelper.WriteFile(resultFile, decompressedBytes);
                        }
                        break;

                    case Algorithm.LZW:
                        if (IsCompressedTextExtension(ext))
                        {
                            var content = FileHelper.ReadFile(path);
                            var decompressed = lzwCompressor.Decompress(content);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.txt");
                            FileHelper.WriteFile(resultFile, decompressed);
                        }
                        else
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var decompressedBytes = lzwCompressor.DecompressBytes(bytes);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.bin");
                            FileHelper.WriteFile(resultFile, decompressedBytes);
                        }
                        break;

                    case Algorithm.Deflate:
                        {
                            var bytes = FileHelper.ReadFileBytes(path);
                            var decompressedBytes = deflateCompressor.DecompressBytes(bytes);
                            resultFile = Path.Combine(examplesDir, Path.GetFileNameWithoutExtension(requestedName) + ".decompressed.bin");
                            FileHelper.WriteFile(resultFile, decompressedBytes);
                        }
                        break;

                    default:
                        throw new InvalidOperationException("Unknown algorithm selected.");
                }

                resultSize = new FileInfo(resultFile).Length;
                var fileType = (IsCompressedTextExtension(ext) || ext == ".txt") ? "📄 Text" : "🖼️ Binary";

                output.Text =
                    $"✅ File: {requestedName}\n" +
                    $"File Type: {fileType}\n" +
                    $"Algorithm: {algorithm}\n" +
                    $"Original Size: {originalSize} bytes\n" +
                    $"Decompressed Size: {resultSize} bytes\n" +
                    $"Status: Success!\n" +
                    $"Saved File: {Path.GetFileName(resultFile)}";
            }
            catch (Exception ex)
            {
                output.Text = $"❌ Error: {ex.Message}";
            }
        }
    }
}