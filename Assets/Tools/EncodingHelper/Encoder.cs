using System.IO;
using System.Text;
using UnityEngine;


namespace Tools.EncodingHelper
{
    public static class Encoder
    {
        public static Encoding DetectEncoding( string filePath, Encoding defaultEncoding = null )
        {
            Debug.Log( "DetectEncoding Check for BOM patterns..." );

            // Read first 4 bytes to check for BOM
            var bom = new byte[4];
            using( var file = new FileStream( filePath, FileMode.Open, FileAccess.Read ) )
            {
                file.Read( bom, 0, 4 );
            }

            // Check for BOM patterns
            if( bom[0] == 0xEF
                && bom[1] == 0xBB
                && bom[2] == 0xBF )
                return Encoding.UTF8;

            if( bom[0] == 0xFF
                && bom[1] == 0xFE )
                return Encoding.Unicode;

            if( bom[0] == 0xFE
                && bom[1] == 0xFF )
                return Encoding.BigEndianUnicode;

            if( bom[0] == 0xFF
                && bom[1] == 0xFE
                && bom[2] == 0x00
                && bom[3] == 0x00 )
                return Encoding.UTF32;

            // If no BOM, try to detect encoding by content
            return DetectEncodingFromContent( filePath, defaultEncoding );
        }


        private static Encoding DetectEncodingFromContent( string filePath, Encoding defaultEncoding )
        {
            Debug.Log( "DetectEncodingFromContent..." );

            byte[] fileContent = File.ReadAllBytes( filePath );

            // Try UTF-8 first
            if( TryDecode( fileContent, Encoding.UTF8 ) )
                return Encoding.UTF8;

            // Try Windows-1251 (Cyrillic)
            if( TryDecode( fileContent, Encoding.GetEncoding( 1251 ) ) )
                return Encoding.GetEncoding( 1251 );

            // Try Windows-1252 (Western European)
            if( TryDecode( fileContent, Encoding.GetEncoding( 1252 ) ) )
                return Encoding.GetEncoding( 1252 );

            // Try system default ANSI encoding
            Encoding ansiEncoding = Encoding.GetEncoding(0);
            if (TryDecode(fileContent, ansiEncoding))
                return ansiEncoding;

            // Fallback
            return defaultEncoding ?? Encoding.UTF8;
        }


        private static bool TryDecode( byte[] bytes, Encoding encoding )
        {
            try
            {
                var enc = (Encoding) encoding.Clone();
                enc.DecoderFallback = DecoderFallback.ExceptionFallback;
                enc.GetString( bytes );
                return true;
            }
            catch
            {
                return false;
            }
        }


        public static string ReadAllTextEncodingAuto( string path )
        {
            var encoding = DetectEncoding( path, Encoding.UTF8 );

            Debug.Log( $"EncodingAuto: {encoding} - {encoding.EncodingName}" );

            return ReadAllText( path, encoding );
        }


        public static string ReadAllText( string path, Encoding encoding )
        {
            string text = File.ReadAllText(path, encoding);
            Debug.Log($"Encoding: {encoding} - {encoding.EncodingName}");
            return text;
        }
    }
}