using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Tools.Encoding;


namespace Tools
{
    public class TextImporterWindow : EditorWindow
    {
        private List<string> _strings = new();
        private Vector2 _scroll;

        [MenuItem( "Tools/Text Importer" )]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TextImporterWindow>();
            wnd.titleContent = new GUIContent( "Duplicates finder" );
            wnd.Show();
        }


        private void OnGUI()
        {
            // GUILayout.Label("Text Importer", _cyrillicBoldLabelStyle);

            // GUILayout.Space(5);

            if( GUILayout.Button( "Load XML" ) )
            {
                string path = EditorUtility.OpenFilePanel( "Select XML file", "", "xml" );
                if( !string.IsNullOrEmpty( path ) )
                    LoadFromXml( path );
            }

            if( GUILayout.Button( "Load JSON" ) )
            {
                string path = EditorUtility.OpenFilePanel( "Select JSON file", "", "json" );
                if( !string.IsNullOrEmpty( path ) )
                    LoadFromJson( path );
            }

            if( GUILayout.Button( "Load TXT" ) )
            {
                string path = EditorUtility.OpenFilePanel( "Select TXT file", "", "txt" );
                if( !string.IsNullOrEmpty( path ) )
                    LoadFromTxt( path );
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

                GUILayout.Space( 5 );
                if( GUILayout.Button( "Clear list" ) )
                    _strings.Clear();
            }
        }


        // ===== Import =====
        private void LoadFromXml( string path )
        {
            try
            {
                string xmlText = EncodingHelper.ReadAllTextAuto( path );
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
                string json = EncodingHelper.ReadAllTextAuto( path );
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
                var lines = EncodingHelper.ReadAllLinesAuto( path );
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