using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using Tools.EncodingHelper;


namespace Tools
{
    public class DuplicatesFinderWindow0 : EditorWindow
    {
        private List<string> _strings = new();
        private Vector2 _scroll;
        private Encoding selectedEncoding = Encoding.UTF8;
        private int selectedEncodingIndex = 0;
        private int _lastEncodingIndex = 0; // to detect changes in dropdown

        // Stores info about the last opened file
        private string _currentFilePath = string.Empty;   // full path
        private string _currentFileName = string.Empty;   // file name without extension
        private string _currentFileExtension = string.Empty; // extension without dot

        private string[] encodingOptions =
            new[] {"UTF-8", "Windows-1251", "Windows-1252", "Unicode", "BigEndianUnicode"};


        [MenuItem( "Tools/Duplicates Finder0" )]
        public static void ShowWindow()
        {
            var wnd = GetWindow<DuplicatesFinderWindow0>();
            wnd.titleContent = new GUIContent( "Duplicates finder" );
            wnd.Show();
        }


        private void OnGUI()
        {
            GUILayout.Label( "Text Importer", EditorStyles.boldLabel );

            // Encoding selection
            GUILayout.Label( "File Encoding:" );
            int newEncodingIndex = EditorGUILayout.Popup( selectedEncodingIndex, encodingOptions );
            if (newEncodingIndex != selectedEncodingIndex)
            {
                selectedEncodingIndex = newEncodingIndex;
                selectedEncoding = GetEncodingFromIndex( selectedEncodingIndex );
                // Reload currently opened file (if any) with the new encoding
                TryReloadCurrentFile();
                _lastEncodingIndex = selectedEncodingIndex;
            }
            else
            {
                // ensure selectedEncoding is in sync even if not changed externally
                selectedEncoding = GetEncodingFromIndex( selectedEncodingIndex );
            }

            GUILayout.Space( 10 );

            GUILayout.BeginHorizontal();
            {
                if( GUILayout.Button( "Load XML" ) )
                {
                    string path = EditorUtility.OpenFilePanel( "Select XML file", "", "xml" );
                    if( !string.IsNullOrEmpty( path ) )
                    {
                        CaptureFileInfo(path);
                        LoadFromXml( path, selectedEncoding );
                    }
                }

                if( GUILayout.Button( "Load JSON" ) )
                {
                    string path = EditorUtility.OpenFilePanel( "Select JSON file", "", "json" );
                    if( !string.IsNullOrEmpty( path ) )
                    {
                        CaptureFileInfo(path);
                        LoadFromJson( path, selectedEncoding );
                    }
                }

                if( GUILayout.Button( "Load TXT" ) )
                {
                    string path = EditorUtility.OpenFilePanel( "Select TXT file", "", "txt" );
                    if( !string.IsNullOrEmpty( path ) )
                    {
                        CaptureFileInfo(path);
                        LoadFromTxt( path, selectedEncoding );
                    }
                }
            }

            GUILayout.EndHorizontal();


            GUILayout.Space( 10 );

            GUILayout.Label( $"Loaded strings: {_strings.Count}" );

            // Show info about currently opened file (if any)
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                GUILayout.Label($"Current file: {_currentFileName}.{_currentFileExtension}");
                EditorGUILayout.SelectableLabel(_currentFilePath, GUILayout.Height(16));
            }

            _scroll = GUILayout.BeginScrollView( _scroll, GUILayout.Height( 300 ) );
            for( int i = 0; i < _strings.Count; i++ )
            {
                EditorGUILayout.LabelField( $"{i + 1}. {_strings[i]}" );
            }

            GUILayout.EndScrollView();

            if( _strings.Count > 0 )
            {
                GUILayout.Space( 10 );
                
                if( GUILayout.Button( "Clear list" ) )
                    _strings.Clear();
                GUILayout.Space( 5 );
                
                GUILayout.Label( "Export:" );

                GUILayout.BeginHorizontal();
                {
                    if( GUILayout.Button( "Export to XML" ) )
                    {
                        string path = EditorUtility.SaveFilePanel( "Save as XML", "_currentFilePath", $"{_currentFileName}.xml", "xml" );
                        if( !string.IsNullOrEmpty( path ) )
                            SaveToXml( path );
                    }

                    if( GUILayout.Button( "Export to JSON" ) )
                    {
                        string path = EditorUtility.SaveFilePanel( "Save as JSON", "_currentFilePath", $"{_currentFileName}.json", "json" );
                        if( !string.IsNullOrEmpty( path ) )
                            SaveToJson( path );
                    }

                    if( GUILayout.Button( "Export to TXT" ) )
                    {
                        string path = EditorUtility.SaveFilePanel( "Save as TXT", "_currentFilePath", $"{_currentFileName}.txt", "txt" );
                        if( !string.IsNullOrEmpty( path ) )
                            SaveToTxt( path );
                    }
                }

                GUILayout.EndHorizontal();

            }
        }


        private Encoding GetEncodingFromIndex( int index )
        {
            switch( index )
            {
                case 0:  return Encoding.UTF8;
                case 1:  return Encoding.GetEncoding( 1251 );
                case 2:  return Encoding.GetEncoding( 1252 );
                case 3:  return Encoding.Unicode;
                case 4:  return Encoding.BigEndianUnicode;
                default: return Encoding.UTF8;
            }
        }

        // Captures file path, name and extension for the last opened file
        private void CaptureFileInfo(string path)
        {
            _currentFilePath = path;
            try
            {
                _currentFileName = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
                var extWithDot = Path.GetExtension(path) ?? string.Empty;
                _currentFileExtension = extWithDot.StartsWith(".") ? extWithDot.Substring(1) : extWithDot;
            }
            catch
            {
                _currentFileName = string.Empty;
                _currentFileExtension = string.Empty;
            }
        }

        // Reloads current file (based on stored path and extension) with current selected encoding
        private void TryReloadCurrentFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || string.IsNullOrEmpty(_currentFileExtension))
                return;

            // Choose loader by extension
            var ext = _currentFileExtension.ToLowerInvariant();
            switch (ext)
            {
                case "xml":
                    LoadFromXml(_currentFilePath, selectedEncoding);
                    break;
                case "json":
                    LoadFromJson(_currentFilePath, selectedEncoding);
                    break;
                case "txt":
                    LoadFromTxt(_currentFilePath, selectedEncoding);
                    break;
                default:
                    // Unknown extension; try a safe default (txt)
                    try
                    {
                        LoadFromTxt(_currentFilePath, selectedEncoding);
                    }
                    catch { }
                    break;
            }
        }

 
        // ===== Import methods with encoding parameter =====
        private void LoadFromXml( string path, Encoding encoding )
        {
            try
            {
                string xmlText = File.ReadAllText( path, encoding );
                using( var reader = new StringReader( xmlText ) )
                {
                    var doc = XDocument.Load( reader );
                    _strings = doc.Descendants( "string" )
                                  .Select( x => x.Value.Trim() )
                                  .Where( x => !string.IsNullOrEmpty( x ) )
                                  .ToList();
                }
            }
            catch( System.Exception ex )
            {
                Debug.LogError( $"XML read error: {ex.Message}" );
            }
        }


        private void LoadFromJson( string path, Encoding encoding )
        {
            try
            {
                string json = File.ReadAllText( path, encoding );
                var arr = JsonUtility.FromJson<StringArrayWrapper>( json );
                _strings = arr?.strings?.ToList() ?? new List<string>();
            }
            catch( System.Exception ex )
            {
                Debug.LogError( $"JSON read error: {ex.Message}" );
            }
        }


        private void LoadFromTxt( string path, Encoding encoding )
        {
            try
            {
                var lines = File.ReadAllLines( path, encoding );
                _strings = lines.Select( l => l.Trim() )
                                .Where( l => !string.IsNullOrEmpty( l ) )
                                .ToList();
            }
            catch( System.Exception ex )
            {
                Debug.LogError( $"TXT read error: {ex.Message}" );
            }
        }


        // ===== Export methods (remain unchanged) =====
        private void SaveToXml( string path )
        {
            try
            {
                var doc = new XDocument( new XElement( "root",
                                                       _strings.Select( s => new XElement( "string", s ) )
                                         ) );

                doc.Save( path );
                AssetDatabase.Refresh();
            }
            catch( System.Exception ex )
            {
                Debug.LogError( $"XML save error: {ex.Message}" );
            }
        }


        private void SaveToJson( string path )
        {
            try
            {
                var wrapper = new StringArrayWrapper {strings = _strings.ToArray()};
                string json = JsonUtility.ToJson( wrapper, true );
                File.WriteAllText( path, json, Encoding.UTF8 );
                AssetDatabase.Refresh();
            }
            catch( System.Exception ex )
            {
                Debug.LogError( $"JSON save error: {ex.Message}" );
            }
        }


        private void SaveToTxt( string path )
        {
            try
            {
                File.WriteAllLines( path, _strings, Encoding.UTF8 );
                AssetDatabase.Refresh();
            }
            catch( System.Exception ex )
            {
                Debug.LogError( $"TXT save error: {ex.Message}" );
            }
        }


        [System.Serializable]
        private class StringArrayWrapper
        {
            public string[] strings;
        }
    }
}
