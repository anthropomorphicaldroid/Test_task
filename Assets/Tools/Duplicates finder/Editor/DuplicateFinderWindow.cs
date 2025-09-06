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
    private List<string> Strings = new List<string>();

    [SerializeField]
    private List<string> FilteredStrings = new List<string>();

    [SerializeField]
    private List<bool> SentenceSelection = new List<bool>();

    private Vector2 _scrollPosition;
    private string _importPath = "";
    private string _exportPath = "";
    private float _similarityThreshold = 0.8f;
    private int _selectedTab = 0;
    private readonly string[] _tabNames = {"Import", "Analysis", "Manual Review", "Export"};

    // Fields for XML structure display and filtering
    private string _xmlStructure = "";
    private string _filterTags = "";
    private Vector2 _structureScrollPosition;

    private readonly string[] _encodingOptions = {"Auto", "UTF-8", "Windows-1251", "Windows-1252", "Unicode", "BigEndianUnicode"};
    private int _selectedEncodingIndex = 0;
    private Encoding _selectedEncoding = Encoding.Default;


    [MenuItem( "Tools/Duplicate Finder" )]
    public static void ShowWindow()
    {
        GetWindow<DuplicateFinderWindow>( "Duplicate Finder" );
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

        // Display XML structure with examples
        if( !string.IsNullOrEmpty( _xmlStructure ) )
        {
            GUILayout.Label( "XML Structure Example:", EditorStyles.boldLabel );
            _structureScrollPosition =
                EditorGUILayout.BeginScrollView( _structureScrollPosition, GUILayout.Height( 200 ) );

            EditorGUILayout.TextArea( _xmlStructure, EditorStyles.wordWrappedLabel );
            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox(
                "This shows the structure with one example of each tag. Use these tag names for filtering below.",
                MessageType.Info );
        }

        // Field for filter tags input
        _filterTags = EditorGUILayout.TextField( "Filter Tags (comma separated)", _filterTags );

        // Encoding selection
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

        GUILayout.Space( 10 );

        if( GUILayout.Button( "Import XML" ) )
        {
            string[] tags = _filterTags.Split( new char[] {','}, System.StringSplitOptions.RemoveEmptyEntries );
            for( int i = 0; i < tags.Length; i++ )
            {
                tags[i] = tags[i].Trim();
            }

            ImportFromXML( _importPath, tags );
        }

        GUILayout.Space( 10 );
        GUILayout.Label( $"Imported Sentences: {Strings.Count}", EditorStyles.boldLabel );
        DrawSentencesList( Strings );
    }


    private void DrawAnalysisTab()
    {
        GUILayout.Label( "Analysis Settings", EditorStyles.boldLabel );

        _similarityThreshold = EditorGUILayout.Slider( "Similarity Threshold",
                                                      _similarityThreshold,
                                                      0.1f,
                                                      1.0f );

        if( GUILayout.Button( "Find Duplicates" ) )
        {
            FindDuplicates();
        }

        GUILayout.Space( 10 );
        GUILayout.Label( $"Filtered Sentences: {FilteredStrings.Count}", EditorStyles.boldLabel );
        DrawSentencesList( FilteredStrings );
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
            ExportToXML( _exportPath, FilteredStrings );
        }

        if( GUILayout.Button( "Export Original XML" ) )
        {
            ExportToXML( _exportPath, Strings );
        }
    }


    private void DrawManualReviewTab()
    {
        GUILayout.Label( "Manual Review", EditorStyles.boldLabel );
        GUILayout.Label( "Select strings to keep (uncheck duplicates):", EditorStyles.helpBox );

        if( SentenceSelection.Count != Strings.Count )
        {
            SentenceSelection = Strings.Select( s => true ).ToList();
        }

        _scrollPosition = EditorGUILayout.BeginScrollView( _scrollPosition );

        for( int i = 0; i < Strings.Count; i++ )
        {
            EditorGUILayout.BeginHorizontal();
            SentenceSelection[i] = EditorGUILayout.Toggle( SentenceSelection[i], GUILayout.Width( 20 ) );
            EditorGUILayout.LabelField( $"{i + 1}. {Strings[i]}",
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
            EditorGUILayout.HelpBox( "No strings to display.", MessageType.Info );
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
            Strings.Clear();

            // Use custom Encoder for proper encoding handling
            string xmlContent = Equals( _selectedEncoding, Encoding.Default )
                                    ? Encoder.ReadAllTextEncodingAuto( path )
                                    : Encoder.ReadAllText( path, _selectedEncoding );

            Debug.Log( xmlContent );

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
                            Strings.Add( node.InnerText.Trim() );
                    }
                }
            }

            FilteredStrings = new List<string>( Strings );
            EditorUtility.DisplayDialog( "Success",
                                         $"Imported {Strings.Count} strings",
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
            Strings.Add( node.Value.Trim() );
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
                                             $"Exported {dataToExport.Count} strings",
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
        // Placeholder for duplicate finding algorithm
        // In real implementation, complex comparison logic would be here
        FilteredStrings = Strings.Distinct().ToList();
        EditorUtility.DisplayDialog( "Info",
                                     $"Found {Strings.Count - FilteredStrings.Count} duplicates",
                                     "OK" );
    }


    private void ApplyManualSelection()
    {
        FilteredStrings = new List<string>();

        for( int i = 0; i < Strings.Count; i++ )
        {
            if( SentenceSelection[i] )
            {
                FilteredStrings.Add( Strings[i] );
            }
        }

        EditorUtility.DisplayDialog( "Success",
                                     $"Filtered to {FilteredStrings.Count} strings",
                                     "OK" );
    }


    [XmlRoot( "Sentences" )]
    public class SentenceData
    {
        [XmlElement( "Sentence" )]
        public List<string> sentences = new List<string>();
    }
}
