using System.IO;

namespace Tools.Encoding
{
    /// <summary>
    /// Helper class for handling text files with different encodings.
    /// Provides automatic encoding detection (UTF-8, UTF-16, Windows-1251, etc.)
    /// and conversion to UTF-8 if necessary.
    /// </summary>
    public static class EncodingHelper
    {
        /// <summary>
        /// Detects the encoding of a file based on BOM (Byte Order Mark).
        /// If no BOM is found, a fallback encoding is used (e.g., Windows-1251).
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="defaultEncoding">Encoding used if BOM is not found. Defaults to UTF-8.</param>
        /// <returns>Detected <see cref="Encoding"/> instance.</returns>
        public static System.Text.Encoding DetectEncoding(string filePath, System.Text.Encoding defaultEncoding = null)
        {
            var bom = new byte[4];
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // UTF-8 BOM
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return System.Text.Encoding.UTF8;

            // UTF-16 LE BOM
            if (bom[0] == 0xFF && bom[1] == 0xFE)
                return System.Text.Encoding.Unicode;

            // UTF-16 BE BOM
            if (bom[0] == 0xFE && bom[1] == 0xFF)
                return System.Text.Encoding.BigEndianUnicode;

            // UTF-32 LE BOM
            if (bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00)
                return System.Text.Encoding.UTF32;

            // Fallback to Windows-1251 (common for Cyrillic text files) or provided encoding
            return defaultEncoding ?? System.Text.Encoding.GetEncoding(1251);
        }

        /// <summary>
        /// Reads a text file with automatic encoding detection.
        /// If the file is not UTF-8, its content is converted to UTF-8.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>File content as a UTF-8 string.</returns>
        public static string ReadAllTextAuto(string path)
        {
            var enc = DetectEncoding(path, System.Text.Encoding.UTF8);
            string text = File.ReadAllText(path, enc);

            // Convert to UTF-8 if necessary
            if (enc != System.Text.Encoding.UTF8)
            {
                byte[] bytes = enc.GetBytes(text);
                text = System.Text.Encoding.UTF8.GetString(bytes);
            }

            return text;
        }

        /// <summary>
        /// Reads a text file line by line with automatic encoding detection.
        /// All lines are converted to UTF-8.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>Array of lines in UTF-8.</returns>
        public static string[] ReadAllLinesAuto(string path)
        {
            string text = ReadAllTextAuto(path);
            return text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        }
    }
}
