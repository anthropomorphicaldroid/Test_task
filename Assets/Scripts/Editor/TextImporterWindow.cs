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

        private GUIStyle _europeanStyle;

        // private GUIStyle _cyrillicButtonStyle;
        // private GUIStyle _cyrillicBoldLabelStyle;


        [MenuItem( "Tools/Text Importer" )]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TextImporterWindow>();
            wnd.titleContent = new GUIContent( "Text Importer" );
            wnd.Show();
        }


        private void InitStyles()
        {
            if( _europeanStyle == null )
            {
                // Используем шрифт, который поддерживает все европейские языки
                Font europeanFont = null;

                // Попробуем найти шрифт, который точно поддерживает все европейские языки
                string[] fontNames = {"Arial", "Times New Roman", "Helvetica", "Verdana"};

                foreach( var fontName in fontNames )
                {
                    var fonts = Resources.FindObjectsOfTypeAll<Font>();
                    europeanFont = fonts.FirstOrDefault( f => f.name == fontName );

                    if( europeanFont != null )
                        break;
                }

                // Если не нашли подходящий шрифт, используем стандартный
                if( europeanFont == null )
                    europeanFont = EditorStyles.label.font;

                // Основной стиль для текста
                _europeanStyle = new GUIStyle( EditorStyles.label );
                _europeanStyle.font = europeanFont;
                _europeanStyle.wordWrap = true;

                // Стиль для кнопок
                // _europeanButtonStyle = new GUIStyle(GUI.skin.button);
                // _europeanButtonStyle.font = europeanFont;

                // Стиль для заголовков
                // _europeanBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                // _europeanBoldLabelStyle.font = europeanFont;
                // _europeanBoldLabelStyle.fontStyle = FontStyle.Bold;
            }
        }


        private void OnGUI()
        {
            InitStyles();

            // Применяем стили ко всем элементам интерфейса
            // GUILayout.Label("Text Importer", _cyrillicBoldLabelStyle);

            // GUILayout.Space(5);

            if( GUILayout.Button( "Load XML" ) )
            {
                string path = EditorUtility.OpenFilePanel( "Выберите XML файл", "", "xml" );
                if( !string.IsNullOrEmpty( path ) )
                    LoadFromXml( path );
            }

            if( GUILayout.Button( "Load JSON" ) )
            {
                string path = EditorUtility.OpenFilePanel( "Выберите JSON файл", "", "json" );
                if( !string.IsNullOrEmpty( path ) )
                    LoadFromJson( path );
            }

            if( GUILayout.Button( "Load TXT" ) )
            {
                string path = EditorUtility.OpenFilePanel( "Выберите TXT файл", "", "txt" );
                if( !string.IsNullOrEmpty( path ) )
                    LoadFromTxt( path );
            }

            GUILayout.Space( 10 );

            GUILayout.Label( $"Загружено строк: {_strings.Count}", _europeanStyle );

            _scroll = GUILayout.BeginScrollView( _scroll, GUILayout.Height( 300 ) );
            for( int i = 0; i < _strings.Count; i++ )
            {
                EditorGUILayout.LabelField( $"{i + 1}. {_strings[i]}", _europeanStyle );
            }

            GUILayout.EndScrollView();

            if( _strings.Count > 0 )
            {
                GUILayout.Space( 10 );

                GUILayout.Label( "Экспорт:" );

                if( GUILayout.Button( "Экспортировать в XML" ) )
                {
                    string path = EditorUtility.SaveFilePanel( "Сохранить как XML", "", "strings.xml", "xml" );
                    if( !string.IsNullOrEmpty( path ) )
                        SaveToXml( path );
                }

                if( GUILayout.Button( "Экспортировать в JSON" ) )
                {
                    string path = EditorUtility.SaveFilePanel( "Сохранить как JSON", "", "strings.json", "json" );
                    if( !string.IsNullOrEmpty( path ) )
                        SaveToJson( path );
                }

                if( GUILayout.Button( "Экспортировать в TXT" ) )
                {
                    string path = EditorUtility.SaveFilePanel( "Сохранить как TXT", "", "strings.txt", "txt" );
                    if( !string.IsNullOrEmpty( path ) )
                        SaveToTxt( path );
                }

                GUILayout.Space( 5 );
                if( GUILayout.Button( "Очистить список" ) )
                    _strings.Clear();
            }
        }


        // ===== Импорт =====
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
                Debug.LogError( $"Ошибка чтения XML: {ex.Message}" );
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
                Debug.LogError( $"Ошибка чтения JSON: {ex.Message}" );
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
                Debug.LogError( $"Ошибка чтения TXT: {ex.Message}" );
            }
        }


        // ===== Экспорт =====
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
                Debug.LogError( $"Ошибка сохранения XML: {ex.Message}" );
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
                Debug.LogError( $"Ошибка сохранения JSON: {ex.Message}" );
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
                Debug.LogError( $"Ошибка сохранения TXT: {ex.Message}" );
            }
        }


        [System.Serializable]
        private class StringArrayWrapper
        {
            public string[] strings;
        }
    }
}
