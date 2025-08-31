using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Tools.EncodingHelper;


namespace Tools
{
    public class DuplicatesFinderWindow : EditorWindow
    {
        private List<string> _strings = new();
        private Vector2 _scroll;

        [MenuItem( "Tools/Duplicates Finder" )]
        public static void ShowWindow()
        {
            var wnd = GetWindow<DuplicatesFinderWindow>();
            wnd.titleContent = new GUIContent( "Duplicates finder" );
            wnd.Show();
        }


        private void OnGUI()
        {
            if( GUILayout.Button( "Load File" ) )
            {
                LoadFileDialog();
            }

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

                GUILayout.Label( "Export:" );

                if( GUILayout.Button( "Save File" ) )
                {
                    SaveFileDialog();
                }

                GUILayout.Space( 5 );
                if( GUILayout.Button( "Clear list" ) )
                    _strings.Clear();
            }
        }


        void LoadFileDialog()
        {
            // Let user pick any file, then decide by extension
            string path = EditorUtility.OpenFilePanel( "Select file", "", "xml" );
            if( !string.IsNullOrEmpty( path ) )
            {
                string ext = System.IO.Path.GetExtension( path )?.TrimStart( '.' ).ToLowerInvariant();
                switch( ext )
                {
                    case "xml":
                        LoadFromXml( path );
                        break;
                    case "json":
                        LoadFromJson( path );
                        break;
                    default:
                        // Any non-xml and non-json files are treated as plain text per requirement
                        LoadFromTxt( path );
                        break;
                }
            }
        }


        void SaveFileDialog()
        {
            // One save button: pick extension by the chosen filename
            string path = EditorUtility.SaveFilePanel( "Save strings", "", "strings", "xml" );
            if( !string.IsNullOrEmpty( path ) )
            {
                string ext = System.IO.Path.GetExtension( path )?.TrimStart( '.' ).ToLowerInvariant();
                switch( ext )
                {
                    case "xml":
                        SaveToXml( path );
                        break;
                    case "json":
                        SaveToJson( path );
                        break;
                    case "txt":
                    case "": // if user omits extension, default to txt
                        if( string.IsNullOrEmpty( ext ) )
                            path += ".txt";
                        SaveToTxt( path );
                        break;
                    default:
                        // For any other extension, save as txt as per requirement of three formats preference
                        SaveToTxt( path );
                        break;
                }
            }
        }


        // ===== Import =====
        private void LoadFromXml( string path )
        {
            try
            {
                string xmlText = Encoder.ReadAllTextAuto( path );
                using( var reader = new StringReader( xmlText ) )
                {
                    var doc = System.Xml.Linq.XDocument.Load( reader );
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


        private void LoadFromJson( string path )
        {
            try
            {
                string json = Encoder.ReadAllTextAuto( path );
                var arr = JsonUtility.FromJson<StringArrayWrapper>( json );
                _strings = arr?.strings?.ToList() ?? new List<string>();
            }
            catch( System.Exception ex )
            {
                Debug.LogError( $"JSON read error: {ex.Message}" );
            }
        }


        private void LoadFromTxt( string path )
        {
            try
            {
                var lines = Encoder.ReadAllLinesAuto( path );
                _strings = lines.Select( l => l.Trim() )
                                .Where( l => !string.IsNullOrEmpty( l ) )
                                .ToList();
            }
            catch( System.Exception ex )
            {
                Debug.LogError( $"TXT read error: {ex.Message}" );
            }
        }


        // ===== Export =====
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
                File.WriteAllText( path, json );
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
                File.WriteAllLines( path, _strings, System.Text.Encoding.UTF8 );
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