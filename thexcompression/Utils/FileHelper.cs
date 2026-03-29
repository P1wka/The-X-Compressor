using System;
using System.IO;

namespace XCompressor.Utils
{
    public static class FileHelper
    {
        public static string ReadFile(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);//for txt files
        }

        public static void WriteFile(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        //for binary files like .png
        public static byte[] ReadFileBytes(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
        }

        public static void WriteFileBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }
    }
}
