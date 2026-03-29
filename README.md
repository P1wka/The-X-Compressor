# The-X-Compressor
A versital data compressor written in C#

## Features
- Supports both text and binary files
- Multiple compression algorithms
- Automatic algorithm selection (Smart Mode)
- Compression ratio & file size analysis
- Terminal-based UI (Terminal.Gui)
- Fast and lightweight

## Purposes
- It sorts the data file according to its type
- It automatically selects the most useful algorithm, or the user can choose the algorithm they want
- Endly, data is compressed

## Algorithms
- RLE
- Huffman (Text-Binary)
- LZ77
- LZW
- Deflate

## Supported File Types
- Text: .txt, .csv, .json
- Binary: .bin, .dat
- Images: .jpg, .png (may not compress efficiently)

## Important Notes
- Some file types like JPG, PNG, MP3 are already compressed. (They'll not work well when you tried to compress)
- Applying compression again may increase file size.
- This is expected behavior and not an error.
