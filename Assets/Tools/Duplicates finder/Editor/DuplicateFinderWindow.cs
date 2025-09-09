using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Text;
using System.Xml;
using Encoder = Tools.EncodingHelper.Encoder;


public class DuplicateFinderWindow : EditorWindow
{
    [SerializeField]
    private List<string> Sentences = new List<string>();

    [SerializeField]
    private List<string> FilteredSentences = new List<string>();

    [SerializeField]
    private List<bool> SentenceSelection = new List<bool>();

    [SerializeField]
    private List<ComparisonStrategy> strategies = new List<ComparisonStrategy>();

    [SerializeField]
    private List<DuplicateGroup> duplicateGroups = new List<DuplicateGroup>();

    private Vector2 _scrollPosition;
    private Vector2 _strategiesScrollPosition = Vector2.zero;
    private Vector2 _duplicatesScrollPosition = Vector2.zero;

    private string _importPath = "";
    private string _exportPath = "";
    private readonly string[] _tabNames = {"Import", "Analysis", "Manual Review", "Export"};
    private int _selectedTab = 0;


    // Fields for XML structure display and filtering
    private string _xmlStructure = "";
    private string _filterTags = "";
    private Vector2 _structureScrollPosition;

    private readonly string[] _encodingOptions =
    {
        "Auto", "UTF-8", "Windows-1251", "Windows-1252", "Unicode", "BigEndianUnicode"
    };

    private int _selectedEncodingIndex = 0;
    private Encoding _selectedEncoding = Encoding.Default;

    private float _similarityThreshold = 0.8f;

    // UI state variables
    private bool _showFileSection = true;
    private bool _showEncodingSection;
    private bool _showFilterSection;
    private bool _showPreviewSection;
    private bool _isDrawSentences;


    [MenuItem( "Tools/Duplicate Finder" )]
    public static void ShowWindow()
    {
        GetWindow<DuplicateFinderWindow>( "Duplicate Finder" );
    }


    void OnEnable()
    {
        _showFileSection = PlayerPrefs.GetInt( "Duplicate Finder.showFileSection" ) == 1;
        _showEncodingSection = PlayerPrefs.GetInt( "Duplicate Finder.showEncodingSection" ) == 1;
        _showFilterSection = PlayerPrefs.GetInt( "Duplicate Finder.showFilterSection" ) == 1;
        _showPreviewSection = PlayerPrefs.GetInt( "Duplicate Finder.showPreviewSection" ) == 1;
        _isDrawSentences = PlayerPrefs.GetInt( "Duplicate Finder.isDrawSentences" ) == 1;
    }


    void OnDisable()
    {
        PlayerPrefs.SetInt( "Duplicate Finder.showFileSection", _showFileSection ? 1 : 0 );
        PlayerPrefs.SetInt( "Duplicate Finder.showEncodingSection", _showEncodingSection ? 1 : 0 );
        PlayerPrefs.SetInt( "Duplicate Finder.showFilterSection", _showFilterSection ? 1 : 0 );
        PlayerPrefs.SetInt( "Duplicate Finder.showPreviewSection", _showPreviewSection ? 1 : 0 );
        PlayerPrefs.SetInt( "Duplicate Finder.isDrawSentences", _isDrawSentences ? 1 : 0 );
    }


    private void OnGUI()
    {
        _selectedTab = GUILayout.Toolbar( _selectedTab, _tabNames );

        switch( _selectedTab )
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

        // File Selection Section with colored background
        EditorGUILayout.BeginVertical( GUI.skin.box );
        {
            EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
            _showFileSection =
                EditorGUILayout.Foldout( _showFileSection, "File Selection", true, EditorStyles.foldout );

            EditorGUILayout.EndHorizontal();

            if( _showFileSection )
            {
                EditorGUI.DrawRect( EditorGUILayout.BeginVertical(), new Color( 0.2f, 0.2f, 0.2f, 0.1f ) );
                {
                    EditorGUILayout.BeginHorizontal();
                    _importPath = EditorGUILayout.TextField( "XML Path", _importPath );

                    if( GUILayout.Button( "Browse", GUILayout.Width( 60 ) ) )
                    {
                        string newPath = EditorUtility.OpenFilePanel( "Select XML file", "", "xml" );
                        if( !string.IsNullOrEmpty( newPath ) )
                        {
                            _importPath = newPath;
                            _xmlStructure = GetXmlStructureExample( _importPath );
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space( 5 );

        // Encoding Selection Section with colored background
        EditorGUILayout.BeginVertical( GUI.skin.box );
        {
            EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
            _showEncodingSection =
                EditorGUILayout.Foldout( _showEncodingSection, "Encoding Settings", true, EditorStyles.foldout );

            EditorGUILayout.EndHorizontal();

            if( _showEncodingSection )
            {
                EditorGUI.DrawRect( EditorGUILayout.BeginVertical(), new Color( 0.2f, 0.3f, 0.2f, 0.1f ) );
                {
                    GUILayout.Label( "File Encoding:" );
                    int newEncodingIndex = EditorGUILayout.Popup( _selectedEncodingIndex, _encodingOptions );
                    if( newEncodingIndex != _selectedEncodingIndex )
                    {
                        _selectedEncodingIndex = newEncodingIndex;
                        _selectedEncoding = GetEncodingFromIndex( _selectedEncodingIndex );
                    }
                    else
                    {
                        // Ensure selectedEncoding is in sync even if not changed externally
                        _selectedEncoding = GetEncodingFromIndex( _selectedEncodingIndex );
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space( 5 );

        // XML Structure Preview Section
        EditorGUILayout.BeginVertical( GUI.skin.box );
        {
            EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
            _showPreviewSection =
                EditorGUILayout.Foldout( _showPreviewSection, "XML Structure Preview", true, EditorStyles.foldout );

            EditorGUILayout.EndHorizontal();

            if( _showPreviewSection )
            {
                EditorGUI.DrawRect( EditorGUILayout.BeginVertical(), new Color( 0.3f, 0.2f, 0.2f, 0.1f ) );
                {
                    _structureScrollPosition =
                        EditorGUILayout.BeginScrollView( _structureScrollPosition, GUILayout.Height( 200 ) );

                    EditorGUILayout.TextArea( _xmlStructure, EditorStyles.wordWrappedLabel );
                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.HelpBox(
                        "This shows the structure with one example of each tag. Use these tag names for filtering above.",
                        MessageType.Info );
                }

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space( 5 );

        // Filter Section with colored background
        EditorGUILayout.BeginVertical( GUI.skin.box );
        {
            EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
            _showFilterSection =
                EditorGUILayout.Foldout( _showFilterSection, "Filter Settings", true, EditorStyles.foldout );

            EditorGUILayout.EndHorizontal();

            if( _showFilterSection )
            {
                EditorGUI.DrawRect( EditorGUILayout.BeginVertical(), new Color( 0.2f, 0.2f, 0.3f, 0.1f ) );
                {
                    // Field for filter tags input
                    _filterTags = EditorGUILayout.TextField( "Filter Tags (comma separated)", _filterTags );
                    EditorGUILayout.HelpBox( "Enter tag names separated by commas to extract specific content",
                                             MessageType.Info );
                }

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();


        GUILayout.Space( 5 );

        if( GUILayout.Button( "Import XML", GUILayout.Height( 30 ) ) )
        {
            string[] tags =
                _filterTags.Split( new char[] {','}, System.StringSplitOptions.RemoveEmptyEntries );

            for( int i = 0; i < tags.Length; i++ )
            {
                tags[i] = tags[i].Trim();
            }

            ImportFromXML( _importPath, tags );
        }

        GUILayout.Space( 10 );

        // Imported Sentences Section
        EditorGUILayout.BeginVertical( GUI.skin.box );
        {
            GUILayout.Label( $"Imported Sentences: {Sentences.Count}", EditorStyles.boldLabel );
            _isDrawSentences = GUILayout.Toggle( _isDrawSentences, "Show lines" );
            if( _isDrawSentences )
            {
                DrawSentencesList( Sentences );
            }
        }

        EditorGUILayout.EndVertical();
    }


    private void DrawAnalysisTab()
    {
        GUILayout.Label( "Analysis Strategies", EditorStyles.boldLabel );

        // Выбор и добавление стратегий
        EditorGUILayout.BeginHorizontal();
        if( GUILayout.Button( "Add Strategy" ) )
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem( new GUIContent( "Exact Match" ), false, () => strategies.Add( new ExactMatchStrategy() ) );
            menu.AddItem( new GUIContent( "Levenshtein Distance" ),
                          false,
                          () => strategies.Add( new LevenshteinStrategy() ) );

            menu.AddItem( new GUIContent( "Jaccard Similarity" ),
                          false,
                          () => strategies.Add( new JaccardStrategy() ) );

            menu.AddItem( new GUIContent( "Cosine Similarity" ),
                          false,
                          () => strategies.Add( new CosineSimilarityStrategy() ) );

            menu.ShowAsContext();
        }

        if( GUILayout.Button( "Clear All" ) )
        {
            strategies.Clear();
        }

        EditorGUILayout.EndHorizontal();

        // Strategies draw
        EditorGUILayout.BeginVertical( GUI.skin.box );
        _strategiesScrollPosition = EditorGUILayout.BeginScrollView( _strategiesScrollPosition );

        for( int i = 0; i < strategies.Count; i++ )
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );

            EditorGUILayout.BeginHorizontal();
            strategies[i].isEnabled = EditorGUILayout.Toggle( strategies[i].isEnabled, GUILayout.Width( 20 ) );
            EditorGUILayout.LabelField( strategies[i].Name, EditorStyles.boldLabel );

            if( GUILayout.Button( "↑", GUILayout.Width( 20 ) )
                && i > 0 )
            {
                var temp = strategies[i - 1];
                strategies[i - 1] = strategies[i];
                strategies[i] = temp;
            }

            if( GUILayout.Button( "↓", GUILayout.Width( 20 ) )
                && i < strategies.Count - 1 )
            {
                var temp = strategies[i + 1];
                strategies[i + 1] = strategies[i];
                strategies[i] = temp;
            }

            if( GUILayout.Button( "×", GUILayout.Width( 20 ) ) )
            {
                strategies.RemoveAt( i );
                i--;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }

            EditorGUILayout.EndHorizontal();

            // Отображаем настройки конкретной стратегии
            strategies[i].DrawSettings();

            EditorGUILayout.EndVertical();
            GUILayout.Space( 5 );
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if( GUILayout.Button( "Find Duplicates" ) )
        {
            FindDuplicates();
        }

        GUILayout.Space( 10 );

        // Duplicates Draw
        EditorGUILayout.BeginVertical( GUI.skin.box );
        GUILayout.Label( $"Found {duplicateGroups.Count} duplicate groups:", EditorStyles.boldLabel );

        // Вычисляем доступную высоту для области дубликатов
        float availableHeight = position.height - GetAnalysisTabNonDuplicatesHeight();
        availableHeight = Mathf.Max( 100, availableHeight ); // Min height 100px

        _duplicatesScrollPosition = EditorGUILayout.BeginScrollView(
            _duplicatesScrollPosition,
            GUILayout.Height( availableHeight )
        );

        if( duplicateGroups.Count > 0 )
        {
            for( int i = 0; i < duplicateGroups.Count; i++ )
            {
                EditorGUILayout.BeginVertical( EditorStyles.helpBox );

                EditorGUILayout.LabelField( $"Group {i + 1} ({duplicateGroups[i].sentences.Count} sentences):",
                                            EditorStyles.boldLabel );

                // Оригинальное предложение (оранжевым)
                var originalStyle = new GUIStyle( EditorStyles.label );
                originalStyle.normal.textColor = new Color( 1f, 0.5f, 0f );

                EditorGUILayout.LabelField( $"{duplicateGroups[i].originalSentence}", originalStyle );

                // Дубликаты (фиолетовым)
                var duplicateStyle = new GUIStyle( EditorStyles.label );
                duplicateStyle.normal.textColor = new Color( 0.1f, 0.8f, 0.7f );

                foreach( var duplicate in duplicateGroups[i].duplicates )
                {
                    EditorGUILayout.LabelField( $"{duplicate}", duplicateStyle );
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space( 5 );
            }
        }
        else
        {
            GUILayout.Label( "No duplicates found", EditorStyles.centeredGreyMiniLabel );
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }


// Вспомогательный метод для расчета высоты всех элементов, кроме области дубликатов
    private float GetAnalysisTabNonDuplicatesHeight()
    {
        // Примерная высота элементов (может потребоваться точная настройка)
        float height = 100; // Базовая высота (отступы, заголовок)

        // Высота кнопок
        height += EditorGUIUtility.singleLineHeight * 2;

        // Высота области стратегий
        float strategiesHeight = 0;
        foreach( var strategy in strategies )
        {
            strategiesHeight += EditorGUIUtility.singleLineHeight * 4; // Примерная высота одной стратегии
        }

        height += strategiesHeight;

        // Высота кнопки "Find Duplicates" и отступов
        height += EditorGUIUtility.singleLineHeight * 3;

        return height;
    }


    private void DrawExportTab()
    {
        GUILayout.Label( "Export Settings", EditorStyles.boldLabel );

        EditorGUILayout.BeginHorizontal();
        _exportPath = EditorGUILayout.TextField( "Export Path", _exportPath );

        if( GUILayout.Button( "Browse", GUILayout.Width( 60 ) ) )
        {
            _exportPath = EditorUtility.SaveFilePanel( "Save XML file", "", "sentences_cleaned.xml", "xml" );
        }

        EditorGUILayout.EndHorizontal();

        if( GUILayout.Button( "Export Cleaned XML" ) )
        {
            ExportToXML( _exportPath, FilteredSentences );
        }

        if( GUILayout.Button( "Export Original XML" ) )
        {
            ExportToXML( _exportPath, Sentences );
        }
    }


    private void DrawManualReviewTab()
    {
        GUILayout.Label( "Manual Review", EditorStyles.boldLabel );
        GUILayout.Label( "Select strings to keep (uncheck duplicates):", EditorStyles.helpBox );

        if( SentenceSelection.Count != Sentences.Count )
        {
            SentenceSelection = Sentences.Select( s => true ).ToList();
        }

        _scrollPosition = EditorGUILayout.BeginScrollView( _scrollPosition );

        for( int i = 0; i < Sentences.Count; i++ )
        {
            EditorGUILayout.BeginHorizontal();
            SentenceSelection[i] = EditorGUILayout.Toggle( SentenceSelection[i], GUILayout.Width( 20 ) );
            EditorGUILayout.LabelField( $"{i + 1}. {Sentences[i]}",
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

        _scrollPosition = EditorGUILayout.BeginScrollView( _scrollPosition,
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
            Sentences.Clear();

            // Use custom Encoder for proper encoding handling
            string xmlContent = Equals( _selectedEncoding, Encoding.Default )
                                    ? Encoder.ReadAllTextEncodingAuto( path )
                                    : Encoder.ReadAllText( path, _selectedEncoding );


            // Create XmlDocument from string
            XmlDocument doc = new XmlDocument();
            doc.LoadXml( xmlContent );

            // Extract all text nodes if no filter tags specified
            if( tagsToFilter == null
                || tagsToFilter.Length == 0 )
            {
                ExtractAllTextNodes( doc.DocumentElement );
            }
            else
            {
                // Extract only nodes with specified tags
                foreach( string tag in tagsToFilter )
                {
                    XmlNodeList nodes = doc.GetElementsByTagName( tag );
                    foreach( XmlNode node in nodes )
                    {
                        if( !string.IsNullOrEmpty( node.InnerText ) )
                            Sentences.Add( node.InnerText.Trim() );
                    }
                }
            }

            FilteredSentences = new List<string>( Sentences );
            EditorUtility.DisplayDialog( "Success",
                                         $"Imported {Sentences.Count} sentences",
                                         "OK" );
        }
        catch( System.Exception e )
        {
            EditorUtility.DisplayDialog( "Error",
                                         $"Failed to import XML: {e.Message}",
                                         "OK" );
        }
    }


    // Method to extract all text nodes
    private void ExtractAllTextNodes( XmlNode node )
    {
        if( node.NodeType == XmlNodeType.Text
            && !string.IsNullOrEmpty( node.Value ) )
        {
            Sentences.Add( node.Value.Trim() );
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
            // Use custom Encoder for proper encoding handling
            string xmlContent = Encoder.ReadAllTextEncodingAuto( path );

            // Create XmlDocument from string
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

        // Add attributes if present
        if( node.Attributes != null
            && node.Attributes.Count > 0 )
        {
            foreach( XmlAttribute attr in node.Attributes )
            {
                result += " " + attr.Name + "=\"" + attr.Value + "\"";
            }
        }

        result += ">";

        // Add text content if present
        bool hasTextContent = false;
        foreach( XmlNode child in node.ChildNodes )
        {
            if( child.NodeType == XmlNodeType.Text
                && !string.IsNullOrWhiteSpace( child.Value ) )
            {
                result += " " + child.Value.Trim();
                hasTextContent = true;
                break; // Only first text element
            }
        }

        if( !hasTextContent )
            result += "\n";

        // Track which tags we've already processed at this level
        HashSet<string> childTags = new HashSet<string>();

        // Process child elements (only one example of each tag)
        foreach( XmlNode child in node.ChildNodes )
        {
            if( child.NodeType == XmlNodeType.Element )
            {
                // Skip if we've already processed a tag with this name at this level
                if( childTags.Contains( child.Name ) )
                    continue;

                childTags.Add( child.Name );

                // Recursively process child element
                result += GetNodeStructureExample( child, indentLevel + 1, processedTags );
            }
        }

        // Closing tag
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
        if( Sentences.Count == 0 )
        {
            EditorUtility.DisplayDialog( "Info", "No sentences to analyze", "OK" );
            return;
        }

        if( strategies.Count == 0 )
        {
            EditorUtility.DisplayDialog( "Info", "No strategies selected", "OK" );
            return;
        }

        try
        {
            // Сбрасываем предыдущие результаты
            duplicateGroups.Clear();
            FilteredSentences = new List<string>( Sentences );

            // Применяем стратегии последовательно
            foreach( var strategy in strategies.Where( s => s.isEnabled ) )
            {
                var result = strategy.FindDuplicates( FilteredSentences );
                FilteredSentences = result.remainingSentences;

                // Добавляем найденные группы дубликатов
                foreach( var group in result.duplicateGroups )
                {
                    duplicateGroups.Add( group );
                }
            }

            EditorUtility.DisplayDialog( "Success",
                                         $"Found {duplicateGroups.Count} duplicate groups",
                                         "OK" );
        }
        catch( Exception e )
        {
            EditorUtility.DisplayDialog( "Error",
                                         $"Failed to find duplicates: {e.Message}",
                                         "OK" );
        }
    }


// Класс для хранения группы дубликатов
    [Serializable]
    public class DuplicateGroup
    {
        public string originalSentence;
        public List<string> duplicates = new List<string>();
        public List<string> sentences => new List<string> {originalSentence}.Concat( duplicates ).ToList();
    }


    // Класс для хранения результатов анализа
    public class AnalysisResult
    {
        public List<string> remainingSentences;
        public List<DuplicateGroup> duplicateGroups;
    }


    // Изменяем базовый класс стратегий
    public abstract class ComparisonStrategy
    {
        public bool isEnabled = true;
        public abstract string Name { get; }
        public abstract void DrawSettings();
        public abstract AnalysisResult FindDuplicates( List<string> sentences );
    }


    [Serializable]
    public class ExactMatchStrategy : ComparisonStrategy
    {
        public override string Name => "Exact Match";


        public override void DrawSettings()
        {
            EditorGUILayout.HelpBox( "Finds exact duplicates (case-sensitive).", MessageType.Info );
        }


        public override AnalysisResult FindDuplicates( List<string> sentences )
        {
            var result = new AnalysisResult
            {
                remainingSentences = new List<string>(), duplicateGroups = new List<DuplicateGroup>()
            };

            var seen = new HashSet<string>();
            var duplicates = new Dictionary<string, List<string>>();

            foreach( var sentence in sentences )
            {
                if( seen.Contains( sentence ) )
                {
                    // Находим оригинал для этого дубликата
                    var original = seen.First( s => s == sentence );
                    if( !duplicates.ContainsKey( original ) )
                    {
                        duplicates[original] = new List<string>();
                    }

                    duplicates[original].Add( sentence );
                }
                else
                {
                    seen.Add( sentence );
                    result.remainingSentences.Add( sentence );
                }
            }

            // Создаем группы дубликатов
            foreach( var kvp in duplicates )
            {
                result.duplicateGroups.Add( new DuplicateGroup {originalSentence = kvp.Key, duplicates = kvp.Value} );
            }

            return result;
        }
    }


    [Serializable]
    public class LevenshteinStrategy : ComparisonStrategy
    {
        public float threshold = 0.8f;
        public bool caseSensitive = false;

        public override string Name => "Levenshtein Distance";


        public override void DrawSettings()
        {
            threshold = EditorGUILayout.Slider( "Similarity Threshold", threshold, 0.1f, 1.0f );
            caseSensitive = EditorGUILayout.Toggle( "Case Sensitive", caseSensitive );
            EditorGUILayout.HelpBox( "Finds similar strings based on edit distance.", MessageType.Info );
        }


        public override AnalysisResult FindDuplicates( List<string> sentences )
        {
            var result = new AnalysisResult
            {
                remainingSentences = new List<string>(), duplicateGroups = new List<DuplicateGroup>()
            };

            if( sentences.Count <= 1 )
            {
                result.remainingSentences = sentences;
                return result;
            }

            var markedForRemoval = new HashSet<int>();
            var duplicateGroups = new Dictionary<int, DuplicateGroup>();

            for( int i = 0; i < sentences.Count; i++ )
            {
                if( markedForRemoval.Contains( i ) )
                    continue;

                result.remainingSentences.Add( sentences[i] );

                for( int j = i + 1; j < sentences.Count; j++ )
                {
                    if( markedForRemoval.Contains( j ) )
                        continue;

                    string a = caseSensitive ? sentences[i] : sentences[i].ToLower();
                    string b = caseSensitive ? sentences[j] : sentences[j].ToLower();

                    float similarity = CalculateLevenshteinSimilarity( a, b );

                    if( similarity >= threshold )
                    {
                        markedForRemoval.Add( j );

                        if( !duplicateGroups.ContainsKey( i ) )
                        {
                            duplicateGroups[i] = new DuplicateGroup {originalSentence = sentences[i]};
                        }

                        duplicateGroups[i].duplicates.Add( sentences[j] );
                    }
                }
            }

            // Добавляем группы дубликатов в результат
            result.duplicateGroups.AddRange( duplicateGroups.Values );

            return result;
        }


        private float CalculateLevenshteinSimilarity( string a, string b )
        {
            // Реализация расчета расстояния Левенштейна
            int[,] matrix = new int[a.Length + 1, b.Length + 1];

            for( int i = 0; i <= a.Length; i++ )
                matrix[i, 0] = i;

            for( int j = 0; j <= b.Length; j++ )
                matrix[0, j] = j;

            for( int i = 1; i <= a.Length; i++ )
            {
                for( int j = 1; j <= b.Length; j++ )
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min( matrix[i - 1, j] + 1, matrix[i, j - 1] + 1 ),
                        matrix[i - 1, j - 1] + cost );
                }
            }

            int maxLength = Math.Max( a.Length, b.Length );
            if( maxLength == 0 )
                return 1.0f;

            return 1.0f - (float) matrix[a.Length, b.Length] / maxLength;
        }
    }


    // Стратегия сходства Жаккара
    [Serializable]
    public class JaccardStrategy : ComparisonStrategy
    {
        public float threshold = 0.7f;
        public int ngramSize = 2;
        public bool useWords = true;

        public override string Name => "Jaccard Similarity";


        public override void DrawSettings()
        {
            threshold = EditorGUILayout.Slider( "Similarity Threshold", threshold, 0.1f, 1.0f );
            useWords = EditorGUILayout.Toggle( "Use Words (instead of n-grams)", useWords );

            if( !useWords )
            {
                ngramSize = EditorGUILayout.IntSlider( "N-Gram Size", ngramSize, 1, 5 );
            }

            EditorGUILayout.HelpBox( "Finds similar strings based on word or n-gram overlap.", MessageType.Info );
        }


        public override AnalysisResult FindDuplicates( List<string> sentences )
        {
            var result = new AnalysisResult
            {
                remainingSentences = new List<string>(), duplicateGroups = new List<DuplicateGroup>()
            };

            if( sentences.Count <= 1 )
            {
                result.remainingSentences = sentences;
                return result;
            }

            // Предварительно вычисляем множества для каждого предложения
            List<HashSet<string>> sets = new List<HashSet<string>>();
            foreach( var sentence in sentences )
            {
                sets.Add( useWords ? CreateWordSet( sentence ) : CreateNGramSet( sentence, ngramSize ) );
            }

            var markedForRemoval = new HashSet<int>();
            var duplicateGroups = new Dictionary<int, DuplicateGroup>();

            for( int i = 0; i < sentences.Count; i++ )
            {
                if( markedForRemoval.Contains( i ) )
                    continue;

                result.remainingSentences.Add( sentences[i] );

                for( int j = i + 1; j < sentences.Count; j++ )
                {
                    if( markedForRemoval.Contains( j ) )
                        continue;

                    float similarity = CalculateJaccardSimilarity( sets[i], sets[j] );

                    if( similarity >= threshold )
                    {
                        markedForRemoval.Add( j );

                        if( !duplicateGroups.ContainsKey( i ) )
                        {
                            duplicateGroups[i] = new DuplicateGroup {originalSentence = sentences[i]};
                        }

                        duplicateGroups[i].duplicates.Add( sentences[j] );
                    }
                }
            }

            // Добавляем группы дубликатов в результат
            result.duplicateGroups.AddRange( duplicateGroups.Values );

            return result;
        }


        private HashSet<string> CreateWordSet( string text )
        {
            // Разбиваем текст на слова, удаляем пустые элементы и приводим к нижнему регистру
            return new HashSet<string>(
                text.Split( new[] {' ', '.', ',', '!', '?', ';', ':', '\t', '\n'},
                            StringSplitOptions.RemoveEmptyEntries )
                    .Select( word => word.ToLowerInvariant() )
            );
        }


        private HashSet<string> CreateNGramSet( string text, int n )
        {
            var ngrams = new HashSet<string>();
            var words = CreateWordSet( text ).ToArray();

            // Создаем n-граммы из слов
            for( int i = 0; i <= words.Length - n; i++ )
            {
                ngrams.Add( string.Join( " ", words, i, n ) );
            }

            // Если n-граммы не создались (мало слов), используем отдельные слова
            if( ngrams.Count == 0
                && words.Length > 0 )
            {
                foreach( var word in words )
                {
                    ngrams.Add( word );
                }
            }

            return ngrams;
        }


        private float CalculateJaccardSimilarity( HashSet<string> setA, HashSet<string> setB )
        {
            if( setA.Count == 0
                && setB.Count == 0 )
                return 1.0f;

            if( setA.Count == 0
                || setB.Count == 0 )
                return 0.0f;

            int intersection = setA.Intersect( setB ).Count();
            int union = setA.Union( setB ).Count();

            return (float) intersection / union;
        }
    }


// Стратегия косинусного сходства
    [Serializable]
    public class CosineSimilarityStrategy : ComparisonStrategy
    {
        public float threshold = 0.75f;
        public bool useTfIdf = true;

        public override string Name => "Cosine Similarity";


        public override void DrawSettings()
        {
            threshold = EditorGUILayout.Slider( "Similarity Threshold", threshold, 0.1f, 1.0f );
            useTfIdf = EditorGUILayout.Toggle( "Use TF-IDF", useTfIdf );
            EditorGUILayout.HelpBox( "Finds similar strings based on vector similarity.", MessageType.Info );
        }


        public override AnalysisResult FindDuplicates( List<string> sentences )
        {
            var result = new AnalysisResult
            {
                remainingSentences = new List<string>(), duplicateGroups = new List<DuplicateGroup>()
            };

            if( sentences.Count <= 1 )
            {
                result.remainingSentences = sentences;
                return result;
            }

            // Создаем словарь всех уникальных слов
            HashSet<string> allWords = new HashSet<string>();
            foreach( var sentence in sentences )
            {
                foreach( var word in Tokenize( sentence ) )
                {
                    allWords.Add( word );
                }
            }

            // Вычисляем IDF для каждого слова (если используется TF-IDF)
            Dictionary<string, float> idfCache = new Dictionary<string, float>();
            if( useTfIdf )
            {
                foreach( var word in allWords )
                {
                    idfCache[word] = CalculateIDF( word, sentences );
                }
            }

            // Создаем векторы для каждого предложения
            List<float[]> vectors = new List<float[]>();
            foreach( var sentence in sentences )
            {
                vectors.Add( CreateVector( sentence, allWords, idfCache, sentences ) );
            }

            var markedForRemoval = new HashSet<int>();
            var duplicateGroups = new Dictionary<int, DuplicateGroup>();

            for( int i = 0; i < sentences.Count; i++ )
            {
                if( markedForRemoval.Contains( i ) )
                    continue;

                result.remainingSentences.Add( sentences[i] );

                for( int j = i + 1; j < sentences.Count; j++ )
                {
                    if( markedForRemoval.Contains( j ) )
                        continue;

                    float similarity = CalculateCosineSimilarity( vectors[i], vectors[j] );

                    if( similarity >= threshold )
                    {
                        markedForRemoval.Add( j );

                        if( !duplicateGroups.ContainsKey( i ) )
                        {
                            duplicateGroups[i] = new DuplicateGroup {originalSentence = sentences[i]};
                        }

                        duplicateGroups[i].duplicates.Add( sentences[j] );
                    }
                }
            }

            // Добавляем группы дубликатов в результат
            result.duplicateGroups.AddRange( duplicateGroups.Values );

            return result;
        }


        private string[] Tokenize( string text )
        {
            // Токенизация текста - разбиение на слова с приведением к нижнему регистру
            return text.ToLowerInvariant()
                       .Split( new[] {' ', '.', ',', '!', '?', ';', ':', '\t', '\n'},
                               StringSplitOptions.RemoveEmptyEntries );
        }


        private float CalculateIDF( string word, List<string> allSentences )
        {
            int documentsWithWord = 0;
            foreach( var sentence in allSentences )
            {
                if( Tokenize( sentence ).Contains( word ) )
                {
                    documentsWithWord++;
                }
            }

            return (float) Math.Log( (float) allSentences.Count / (1 + documentsWithWord) );
        }


        private float[] CreateVector(
            string sentence,
            HashSet<string> allWords,
            Dictionary<string, float> idfCache,
            List<string> allSentences
        )
        {
            float[] vector = new float[allWords.Count];
            var words = Tokenize( sentence );
            var wordCounts = words.GroupBy( w => w )
                                  .ToDictionary( g => g.Key, g => g.Count() );

            int index = 0;
            foreach( var word in allWords )
            {
                if( wordCounts.TryGetValue( word, out int count ) )
                {
                    if( useTfIdf )
                    {
                        // TF (Term Frequency) * IDF (Inverse Document Frequency)
                        float tf = (float) count / words.Length;
                        vector[index] = tf * idfCache[word];
                    }
                    else
                    {
                        // Просто частота слова
                        vector[index] = count;
                    }
                }

                index++;
            }

            return vector;
        }


        private float CalculateCosineSimilarity( float[] vectorA, float[] vectorB )
        {
            float dotProduct = 0;
            float magnitudeA = 0;
            float magnitudeB = 0;

            for( int i = 0; i < vectorA.Length; i++ )
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            magnitudeA = (float) Math.Sqrt( magnitudeA );
            magnitudeB = (float) Math.Sqrt( magnitudeB );

            if( magnitudeA == 0
                || magnitudeB == 0 )
                return 0;

            return dotProduct / (magnitudeA * magnitudeB);
        }
    }


    private void ApplyManualSelection()
    {
        FilteredSentences = new List<string>();

        for( int i = 0; i < Sentences.Count; i++ )
        {
            if( SentenceSelection[i] )
            {
                FilteredSentences.Add( Sentences[i] );
            }
        }

        EditorUtility.DisplayDialog( "Success",
                                     $"Filtered to {FilteredSentences.Count} sentences",
                                     "OK" );
    }


    [XmlRoot( "Sentences" )]
    public class SentenceData
    {
        [XmlElement( "Sentence" )]
        public List<string> sentences = new List<string>();
    }
}
