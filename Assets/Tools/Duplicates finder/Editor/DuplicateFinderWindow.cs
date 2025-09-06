using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Text;
using System.Xml;
using Encoder = Tools.EncodingHelper.Encoder; // Добавляем для работы с XmlDocument


public class DuplicateFinderWindow : EditorWindow
{
    [SerializeField]
    private List<string> sentences = new List<string>();

    [SerializeField]
    private List<string> filteredSentences = new List<string>();

    [SerializeField]
    private List<bool> sentenceSelection = new List<bool>();

    private Vector2 scrollPosition;
    private string importPath = "";
    private string exportPath = "";
    private float similarityThreshold = 0.8f;
    private int selectedTab = 0;
    private string[] tabNames = {"Import", "Analysis", "Manual Review", "Export"};

    // Новые поля для отображения структуры XML и фильтрации
    private string xmlStructure = "";
    private string filterTags = "";
    private Vector2 structureScrollPosition;

    private string[] encodingOptions = {"Auto", "UTF-8", "Windows-1251", "Windows-1252", "Unicode", "BigEndianUnicode"};

    private int selectedEncodingIndex = 0;
    private Encoding selectedEncoding = Encoding.Default;


    [MenuItem( "Tools/Duplicate Finder" )]
    public static void ShowWindow()
    {
        GetWindow<DuplicateFinderWindow>( "Duplicate Finder" );
    }


    private void OnGUI()
    {
        selectedTab = GUILayout.Toolbar( selectedTab, tabNames );

        switch( selectedTab )
        {
            case 0: DrawImportTab(); break;
            case 1: DrawAnalysisTab(); break;
            case 2: DrawManualReviewTab(); break;
            case 3: DrawExportTab(); break;
        }
    }


    private void DrawImportTab()
    {
        GUILayout.Label( "Import Settings", EditorStyles.boldLabel );

        EditorGUILayout.BeginHorizontal();
        importPath = EditorGUILayout.TextField( "XML Path", importPath );

        if( GUILayout.Button( "Browse", GUILayout.Width( 60 ) ) )
        {
            string newPath = EditorUtility.OpenFilePanel( "Select XML file", "", "xml" );
            if( !string.IsNullOrEmpty( newPath ) )
            {
                importPath = newPath;
                xmlStructure = GetXmlStructureExample( importPath );
            }
        }

        EditorGUILayout.EndHorizontal();

        // Отображение структуры XML с примерами
        if( !string.IsNullOrEmpty( xmlStructure ) )
        {
            GUILayout.Label( "XML Structure Example:", EditorStyles.boldLabel );
            structureScrollPosition =
                EditorGUILayout.BeginScrollView( structureScrollPosition, GUILayout.Height( 200 ) );

            EditorGUILayout.TextArea( xmlStructure, EditorStyles.wordWrappedLabel );
            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox(
                "This shows the structure with one example of each tag. Use these tag names for filtering below.",
                MessageType.Info );
        }

        // Поле для ввода тегов фильтрации
        filterTags = EditorGUILayout.TextField( "Filter Tags (comma separated)", filterTags );

        // Encoding selection
        GUILayout.Label( "File Encoding:" );
        int newEncodingIndex = EditorGUILayout.Popup( selectedEncodingIndex, encodingOptions );
        if( newEncodingIndex != selectedEncodingIndex )
        {
            selectedEncodingIndex = newEncodingIndex;
            selectedEncoding = GetEncodingFromIndex( selectedEncodingIndex );
        }
        else
        {
            // ensure selectedEncoding is in sync even if not changed externally
            selectedEncoding = GetEncodingFromIndex( selectedEncodingIndex );
        }

        GUILayout.Space( 10 );


        if( GUILayout.Button( "Import XML" ) )
        {
            string[] tags = filterTags.Split( new char[] {','}, System.StringSplitOptions.RemoveEmptyEntries );
            for( int i = 0; i < tags.Length; i++ )
            {
                tags[i] = tags[i].Trim();
            }

            ImportFromXML( importPath, tags );
        }

        GUILayout.Space( 10 );
        GUILayout.Label( $"Imported Sentences: {sentences.Count}", EditorStyles.boldLabel );
        DrawSentencesList( sentences );
    }


    private void DrawAnalysisTab()
    {
        GUILayout.Label( "Analysis Settings", EditorStyles.boldLabel );

        similarityThreshold = EditorGUILayout.Slider( "Similarity Threshold",
                                                      similarityThreshold,
                                                      0.1f,
                                                      1.0f );

        if( GUILayout.Button( "Find Duplicates" ) )
        {
            FindDuplicates();
        }

        GUILayout.Space( 10 );
        GUILayout.Label( $"Filtered Sentences: {filteredSentences.Count}", EditorStyles.boldLabel );
        DrawSentencesList( filteredSentences );
    }


    private void DrawExportTab()
    {
        GUILayout.Label( "Export Settings", EditorStyles.boldLabel );

        EditorGUILayout.BeginHorizontal();
        exportPath = EditorGUILayout.TextField( "Export Path", exportPath );

        if( GUILayout.Button( "Browse", GUILayout.Width( 60 ) ) )
        {
            exportPath = EditorUtility.SaveFilePanel( "Save XML file", "", "sentences_cleaned.xml", "xml" );
        }

        EditorGUILayout.EndHorizontal();

        if( GUILayout.Button( "Export Cleaned XML" ) )
        {
            ExportToXML( exportPath, filteredSentences );
        }

        if( GUILayout.Button( "Export Original XML" ) )
        {
            ExportToXML( exportPath, sentences );
        }
    }


    private void DrawManualReviewTab()
    {
        GUILayout.Label( "Manual Review", EditorStyles.boldLabel );
        GUILayout.Label( "Select sentences to keep (uncheck duplicates):", EditorStyles.helpBox );

        if( sentenceSelection.Count != sentences.Count )
        {
            sentenceSelection = sentences.Select( s => true ).ToList();
        }

        scrollPosition = EditorGUILayout.BeginScrollView( scrollPosition );

        for( int i = 0; i < sentences.Count; i++ )
        {
            EditorGUILayout.BeginHorizontal();
            sentenceSelection[i] = EditorGUILayout.Toggle( sentenceSelection[i], GUILayout.Width( 20 ) );
            EditorGUILayout.LabelField( $"{i + 1}. {sentences[i]}",
                                        EditorStyles.textArea,
                                        GUILayout.Height( EditorGUIUtility.singleLineHeight * 2 ) );

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if( GUILayout.Button( "Apply Selection" ) )
        {
            ApplyManualSelection();
        }
    }


    private void DrawSentencesList( List<string> sentencesList )
    {
        if( sentencesList.Count == 0 )
        {
            EditorGUILayout.HelpBox( "No sentences to display.", MessageType.Info );
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView( scrollPosition,
                                                          GUILayout.Height( 300 ) );

        for( int i = 0; i < sentencesList.Count; i++ )
        {
            EditorGUILayout.LabelField( $"{i + 1}. {sentencesList[i]}",
                                        EditorStyles.textArea,
                                        GUILayout.Height( EditorGUIUtility.singleLineHeight * 2 ) );
        }

        EditorGUILayout.EndScrollView();
    }


    private void ImportFromXML( string path, string[] tagsToFilter )
    {
        if( string.IsNullOrEmpty( path )
            || !File.Exists( path ) )
        {
            EditorUtility.DisplayDialog( "Error", "Invalid file path", "OK" );
            return;
        }

        try
        {
            sentences.Clear();

            // Используем ваш Encoder для чтения файла с правильной кодировкой
            string xmlContent = Equals( selectedEncoding, Encoding.Default )
                                    ? Encoder.ReadAllTextEncodingAuto( path )
                                    : Encoder.ReadAllText( path, selectedEncoding );

            Debug.Log( xmlContent );

            // Создаем XmlDocument из строки
            XmlDocument doc = new XmlDocument();
            doc.LoadXml( xmlContent );

            // Если теги для фильтрации не указаны, извлекаем все текстовые узлы
            if( tagsToFilter == null
                || tagsToFilter.Length == 0 )
            {
                ExtractAllTextNodes( doc.DocumentElement );
            }
            else
            {
                // Извлекаем только узлы с указанными тегами
                foreach( string tag in tagsToFilter )
                {
                    XmlNodeList nodes = doc.GetElementsByTagName( tag );
                    foreach( XmlNode node in nodes )
                    {
                        if( !string.IsNullOrEmpty( node.InnerText ) )
                            sentences.Add( node.InnerText.Trim() );
                    }
                }
            }

            filteredSentences = new List<string>( sentences );
            EditorUtility.DisplayDialog( "Success",
                                         $"Imported {sentences.Count} sentences",
                                         "OK" );
        }
        catch( System.Exception e )
        {
            EditorUtility.DisplayDialog( "Error",
                                         $"Failed to import XML: {e.Message}",
                                         "OK" );
        }
    }


    // Новый метод для извлечения всех текстовых узлов
    private void ExtractAllTextNodes( XmlNode node )
    {
        if( node.NodeType == XmlNodeType.Text
            && !string.IsNullOrEmpty( node.Value ) )
        {
            sentences.Add( node.Value.Trim() );
        }

        foreach( XmlNode child in node.ChildNodes )
        {
            ExtractAllTextNodes( child );
        }
    }


    private string GetXmlStructureExample( string path )
    {
        if( string.IsNullOrEmpty( path )
            || !File.Exists( path ) )
            return "Invalid file path";

        try
        {
            // Используем ваш Encoder для чтения файла с правильной кодировкой
            string xmlContent = Encoder.ReadAllTextEncodingAuto( path );

            // Создаем XmlDocument из строки
            XmlDocument doc = new XmlDocument();
            doc.LoadXml( xmlContent );

            return GetNodeStructureExample( doc.DocumentElement, 0, new HashSet<string>() );
        }
        catch( System.Exception e )
        {
            return $"Error reading XML structure: {e.Message}";
        }
    }


    private string GetNodeStructureExample( XmlNode node, int indentLevel, HashSet<string> processedTags )
    {
        string indent = new string( ' ', indentLevel * 2 );
        string result = indent + "<" + node.Name;

        // Добавляем атрибуты, если они есть
        if( node.Attributes != null
            && node.Attributes.Count > 0 )
        {
            foreach( XmlAttribute attr in node.Attributes )
            {
                result += " " + attr.Name + "=\"" + attr.Value + "\"";
            }
        }

        result += ">";

        // Добавляем текстовое содержимое, если оно есть
        bool hasTextContent = false;
        foreach( XmlNode child in node.ChildNodes )
        {
            if( child.NodeType == XmlNodeType.Text
                && !string.IsNullOrWhiteSpace( child.Value ) )
            {
                result += " " + child.Value.Trim();
                hasTextContent = true;
                break; // Только первый текстовый элемент
            }
        }

        if( !hasTextContent )
            result += "\n";

        // Отслеживаем, какие теги мы уже обработали на этом уровне
        HashSet<string> childTags = new HashSet<string>();

        // Обрабатываем дочерние элементы (только по одному примеру каждого тега)
        foreach( XmlNode child in node.ChildNodes )
        {
            if( child.NodeType == XmlNodeType.Element )
            {
                // Пропускаем, если уже обработали тег с таким именем на этом уровне
                if( childTags.Contains( child.Name ) )
                    continue;

                childTags.Add( child.Name );

                // Рекурсивно обрабатываем дочерний элемент
                result += GetNodeStructureExample( child, indentLevel + 1, processedTags );
            }
        }

        // Закрывающий тег
        if( !hasTextContent )
        {
            result += indent + "</" + node.Name + ">\n";
        }
        else
        {
            result += "</" + node.Name + ">\n";
        }

        return result;
    }


    private Encoding GetEncodingFromIndex( int index )
    {
        switch( index )
        {
            case 0:  return Encoding.Default;
            case 1:  return Encoding.UTF8;
            case 2:  return Encoding.GetEncoding( 1251 );
            case 3:  return Encoding.GetEncoding( 1252 );
            case 4:  return Encoding.Unicode;
            case 5:  return Encoding.BigEndianUnicode;
            default: return Encoding.UTF8;
        }
    }


    private void CollectUniqueTags( XmlNode node, HashSet<string> uniqueTags )
    {
        if( node.NodeType == XmlNodeType.Element )
        {
            uniqueTags.Add( node.Name );
        }

        foreach( XmlNode child in node.ChildNodes )
        {
            CollectUniqueTags( child, uniqueTags );
        }
    }


    private void ExportToXML( string path, List<string> dataToExport )
    {
        if( string.IsNullOrEmpty( path ) )
        {
            EditorUtility.DisplayDialog( "Error", "Invalid file path", "OK" );
            return;
        }

        try
        {
            SentenceData data = new SentenceData();
            data.sentences = dataToExport;

            XmlSerializer serializer = new XmlSerializer( typeof( SentenceData ) );
            using( FileStream stream = new FileStream( path, FileMode.Create ) )
            {
                serializer.Serialize( stream, data );
                EditorUtility.DisplayDialog( "Success",
                                             $"Exported {dataToExport.Count} sentences",
                                             "OK" );
            }
        }
        catch( System.Exception e )
        {
            EditorUtility.DisplayDialog( "Error",
                                         $"Failed to export XML: {e.Message}",
                                         "OK" );
        }
    }


    private void FindDuplicates()
    {
        // Заглушка для алгоритма поиска дубликатов
        // В реальной реализации здесь будет сложная логика сравнения
        filteredSentences = sentences.Distinct().ToList();
        EditorUtility.DisplayDialog( "Info",
                                     $"Found {sentences.Count - filteredSentences.Count} duplicates",
                                     "OK" );
    }


    private void ApplyManualSelection()
    {
        filteredSentences = new List<string>();

        for( int i = 0; i < sentences.Count; i++ )
        {
            if( sentenceSelection[i] )
            {
                filteredSentences.Add( sentences[i] );
            }
        }

        EditorUtility.DisplayDialog( "Success",
                                     $"Filtered to {filteredSentences.Count} sentences",
                                     "OK" );
    }


    [XmlRoot( "Sentences" )]
    public class SentenceData
    {
        [XmlElement( "Sentence" )]
        public List<string> sentences = new List<string>();
    }
}
