using System;
using System.IO;
using System.Text;

namespace Tools.EncodingHelper
{
    public static class Encoder
    {
        public static Encoding DetectEncoding(string filePath, Encoding defaultEncoding = null)
        {
            // Read first 4 bytes to check for BOM
            var bom = new byte[4];
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Check for BOM patterns
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return Encoding.UTF8;

            if (bom[0] == 0xFF && bom[1] == 0xFE)
                return Encoding.Unicode;

            if (bom[0] == 0xFE && bom[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            if (bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00)
                return Encoding.UTF32;

            // If no BOM, try to detect encoding by content
            return DetectEncodingFromContent(filePath, defaultEncoding);
        }

        private static Encoding DetectEncodingFromContent(string filePath, Encoding defaultEncoding)
        {
            // Read the entire file content
            byte[] fileContent = File.ReadAllBytes(filePath);
            
            // Try to detect encoding using .NET's Encoding class
            try
            {
                // Try UTF-8 first (most common)
                string utf8Text = Encoding.UTF8.GetString(fileContent);
                if (!ContainsInvalidUtf8Chars(utf8Text))
                    return Encoding.UTF8;

                // Try Windows-1251 (Cyrillic)
                string win1251Text = Encoding.GetEncoding(1251).GetString(fileContent);
                if (!ContainsInvalidChars(win1251Text))
                    return Encoding.GetEncoding(1251);

                // Try Windows-1252 (Western European)
                string win1252Text = Encoding.GetEncoding(1252).GetString(fileContent);
                if (!ContainsInvalidChars(win1252Text))
                    return Encoding.GetEncoding(1252);
            }
            catch
            {
                // Fall through to default encoding
            }

            // Fallback to provided default or UTF-8
            return defaultEncoding ?? Encoding.UTF8;
        }

        private static bool ContainsInvalidUtf8Chars(string text)
        {
            // Check for UTF-8 replacement characters which indicate decoding errors
            return text.Contains("�");
        }

        private static bool ContainsInvalidChars(string text)
        {
            // Check for common invalid characters in single-byte encodings
            return text.Contains("\0") || text.Contains("�");
        }

        public static string ReadAllTextAuto(string path)
        {
            var enc = DetectEncoding(path, Encoding.UTF8);
            string text = File.ReadAllText(path, enc);

            // Always convert to UTF-8 for consistent handling
            if (enc != Encoding.UTF8)
            {
                byte[] bytes = enc.GetBytes(text);
                text = Encoding.UTF8.GetString(bytes);
            }

            return text;
        }

        public static string[] ReadAllLinesAuto(string path)
        {
            string text = ReadAllTextAuto(path);
            return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }
    }
}