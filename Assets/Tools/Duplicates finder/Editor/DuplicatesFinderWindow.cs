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
    public class DuplicatesFinderWindow : EditorWindow
    {
        private List<string> _strings = new();
        private Vector2 _scroll;
        private Encoding selectedEncoding = Encoding.UTF8;
        private int selectedEncodingIndex = 0;

        private string[] encodingOptions =
            new[] {"UTF-8", "Windows-1251", "Windows-1252", "Unicode", "BigEndianUnicode"};


        [MenuItem( "Tools/Duplicates Finder" )]
        public static void ShowWindow()
        {
            var wnd = GetWindow<DuplicatesFinderWindow>();
            wnd.titleContent = new GUIContent( "Duplicates finder" );
            wnd.Show();
        }


        private void OnGUI()
        {
            GUILayout.Label( "Text Importer", EditorStyles.boldLabel );

            // Encoding selection
            GUILayout.Label( "File Encoding:" );
            selectedEncodingIndex = EditorGUILayout.Popup( selectedEncodingIndex, encodingOptions );
            selectedEncoding = GetEncodingFromIndex( selectedEncodingIndex );

            GUILayout.Space( 10 );

            GUILayout.BeginHorizontal();
            {
                if( GUILayout.Button( "Load XML" ) )
                {
                    string path = EditorUtility.OpenFilePanel( "Select XML file", "", "xml" );
                    if( !string.IsNullOrEmpty( path ) )
                        LoadFromXml( path, selectedEncoding );
                }

                if( GUILayout.Button( "Load JSON" ) )
                {
                    string path = EditorUtility.OpenFilePanel( "Select JSON file", "", "json" );
                    if( !string.IsNullOrEmpty( path ) )
                        LoadFromJson( path, selectedEncoding );
                }

                if( GUILayout.Button( "Load TXT" ) )
                {
                    string path = EditorUtility.OpenFilePanel( "Select TXT file", "", "txt" );
                    if( !string.IsNullOrEmpty( path ) )
                        LoadFromTxt( path, selectedEncoding );
                }
            }

            GUILayout.EndHorizontal();


            GUILayout.Space( 10 );

            GUILayout.Label( $"Loaded strings: {_strings.Count}" );

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
                        string path = EditorUtility.SaveFilePanel( "Save as XML", "", "strings.xml", "xml" );
                        if( !string.IsNullOrEmpty( path ) )
                            SaveToXml( path );
                    }

                    if( GUILayout.Button( "Export to JSON" ) )
                    {
                        string path = EditorUtility.SaveFilePanel( "Save as JSON", "", "strings.json", "json" );
                        if( !string.IsNullOrEmpty( path ) )
                            SaveToJson( path );
                    }

                    if( GUILayout.Button( "Export to TXT" ) )
                    {
                        string path = EditorUtility.SaveFilePanel( "Save as TXT", "", "strings.txt", "txt" );
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
