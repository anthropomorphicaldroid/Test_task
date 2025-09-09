using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DuplicateFinder.Utilities;
using DuplicateFinder.Strategies;
using DuplicateFinder.Data;


public class DuplicateFinderWindow : EditorWindow
{
#region Private Members
    private List<string> _sentences = new List<string>();
    private List<string> _filteredSentences = new List<string>();

    private List<ComparisonStrategyBase> _strategies = new List<ComparisonStrategyBase>();
    private List<DuplicateGroup> _duplicateGroups = new List<DuplicateGroup>();

    // UI state variables
    private Vector2 _scrollPosition;
    private Vector2 _strategiesScrollPosition = Vector2.zero;
    private Vector2 _duplicatesScrollPosition = Vector2.zero;
    private string _importPath = "";
    private string _exportPath = "";
    private readonly string[] _tabNames = {"Import", "Analysis", "Export"};
    private int _selectedTab = 0;
    private string _xmlStructure = "";
    private string _filterTags = "";
    private Vector2 _structureScrollPosition;
    private int _selectedEncodingIndex = 0;
    private Encoding _selectedEncoding = Encoding.Default;

    private bool _showFileSection = true;
    private bool _showEncodingSection;
    private bool _showFilterSection;
    private bool _showPreviewSection;
    private bool _isDrawSentences;

    private bool _showExportSection = true;
    private string _exportTag = "Sentence";

    private readonly string[] _encodingOptions =
    {
        "Auto", "UTF-8", "Windows-1251", "Windows-1252", "Unicode", "BigEndianUnicode"
    };
#endregion


#region Unity Methods
    [MenuItem( "Tools/Duplicate Finder" )]
    public static void ShowWindow()
    {
        GetWindow<DuplicateFinderWindow>( "Duplicate Finder" );
    }


    private void OnEnable()
    {
        // Load UI state from PlayerPrefs
        _showFileSection = PlayerPrefs.GetInt( "DuplicateFinder.showFileSection" ) == 1;
        _showEncodingSection = PlayerPrefs.GetInt( "DuplicateFinder.showEncodingSection" ) == 1;
        _showFilterSection = PlayerPrefs.GetInt( "DuplicateFinder.showFilterSection" ) == 1;
        _showPreviewSection = PlayerPrefs.GetInt( "DuplicateFinder.showPreviewSection" ) == 1;
        _isDrawSentences = PlayerPrefs.GetInt( "DuplicateFinder.isDrawSentences" ) == 1;
        _showExportSection = PlayerPrefs.GetInt( "DuplicateFinder.showExportSection" ) == 1;
    }


    private void OnDisable()
    {
        // Save UI state to PlayerPrefs
        PlayerPrefs.SetInt( "DuplicateFinder.showFileSection", _showFileSection ? 1 : 0 );
        PlayerPrefs.SetInt( "DuplicateFinder.showEncodingSection", _showEncodingSection ? 1 : 0 );
        PlayerPrefs.SetInt( "DuplicateFinder.showFilterSection", _showFilterSection ? 1 : 0 );
        PlayerPrefs.SetInt( "DuplicateFinder.showPreviewSection", _showPreviewSection ? 1 : 0 );
        PlayerPrefs.SetInt( "DuplicateFinder.isDrawSentences", _isDrawSentences ? 1 : 0 );
        PlayerPrefs.SetInt( "DuplicateFinder.showExportSection", _showExportSection ? 1 : 0 );
    }


    private void OnGUI()
    {
        _selectedTab = GUILayout.Toolbar( _selectedTab, _tabNames, GUILayout.Height( 30 ) );

        switch( _selectedTab )
        {
            case 0: DrawImportTab(); break;
            case 1: DrawAnalysisTab(); break;
            case 2: DrawExportTab(); break;
        }
    }
#endregion


#region Draw Methods
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
                            _xmlStructure = XmlHelper.GetXmlStructureExample( _importPath );
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
            string[] tags = _filterTags.Split( new char[] {','}, System.StringSplitOptions.RemoveEmptyEntries );
            for( int i = 0; i < tags.Length; i++ )
            {
                tags[i] = tags[i].Trim();
            }

            _sentences = XmlHelper.ImportFromXml( _importPath,
                                                  tags,
                                                  _selectedEncoding == Encoding.Default ? null : _selectedEncoding );

            _filteredSentences = _sentences;
            if( _sentences.Count != 0 )
            {
                EditorUtility.DisplayDialog( "Success", $"Imported {_sentences.Count} sentences", "OK" );
            }
        }

        GUILayout.Space( 10 );

        // Imported Sentences Section
        EditorGUILayout.BeginVertical( GUI.skin.box );
        {
            GUILayout.Label( $"Imported Sentences: {_sentences.Count}", EditorStyles.boldLabel );
            _isDrawSentences = GUILayout.Toggle( _isDrawSentences, "Show lines" );

            if( _isDrawSentences )
            {
                // Calculate available height for the list, same as in the Analysis tab
                float availableHeight = position.height - GetImportTabNonListHeight();
                availableHeight = Mathf.Max( 100, availableHeight ); // Minimum height 100px

                DrawSentencesList( _sentences, availableHeight );
            }
        }

        EditorGUILayout.EndVertical();
    }


    private void DrawAnalysisTab()
    {
        GUILayout.Label( "Analysis Statistics", EditorStyles.boldLabel );

        // Statistics of imported and filtered sentences
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label( $"Imported: {_sentences.Count}", EditorStyles.miniLabel );
        GUILayout.Label( $"Filtered: {_filteredSentences.Count}", EditorStyles.miniLabel );
        GUILayout.Label( $"Removed: {_sentences.Count - _filteredSentences.Count}", EditorStyles.miniLabel );
        EditorGUILayout.EndHorizontal();

        GUILayout.Space( 10 );
        GUILayout.Label( "Analysis Strategies", EditorStyles.boldLabel );

        // Selecting and adding strategies
        EditorGUILayout.BeginHorizontal();
        if( GUILayout.Button( "Add Strategy", GUILayout.Height( 30 ) ) )
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem( new GUIContent( "Exact Match" ), false, () => _strategies.Add( new ExactMatchStrategy() ) );
            menu.AddItem( new GUIContent( "Levenshtein Distance" ),
                          false,
                          () => _strategies.Add( new LevenshteinStrategy() ) );

            menu.AddItem( new GUIContent( "Jaccard Similarity" ),
                          false,
                          () => _strategies.Add( new JaccardStrategy() ) );

            menu.AddItem( new GUIContent( "Cosine Similarity" ),
                          false,
                          () => _strategies.Add( new CosineSimilarityStrategy() ) );

            menu.ShowAsContext();
        }

        if( GUILayout.Button( "Clear All", GUILayout.Height( 30 ) ) )
        {
            _strategies.Clear();
        }

        EditorGUILayout.EndHorizontal();

        // Strategies draw
        EditorGUILayout.BeginVertical( GUI.skin.box );
        _strategiesScrollPosition = EditorGUILayout.BeginScrollView( _strategiesScrollPosition );

        for( int i = 0; i < _strategies.Count; i++ )
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );

            EditorGUILayout.BeginHorizontal();
            _strategies[i].IsEnabled = EditorGUILayout.Toggle( _strategies[i].IsEnabled, GUILayout.Width( 20 ) );
            EditorGUILayout.LabelField( _strategies[i].Name, EditorStyles.boldLabel );

            if( GUILayout.Button( "↑", GUILayout.Width( 20 ) )
                && i > 0 )
            {
                (_strategies[i - 1], _strategies[i]) = (_strategies[i], _strategies[i - 1]);
            }

            if( GUILayout.Button( "↓", GUILayout.Width( 20 ) )
                && i < _strategies.Count - 1 )
            {
                (_strategies[i + 1], _strategies[i]) = (_strategies[i], _strategies[i + 1]);
            }

            if( GUILayout.Button( "×", GUILayout.Width( 20 ) ) )
            {
                _strategies.RemoveAt( i );
                i--;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }

            EditorGUILayout.EndHorizontal();

            // Display strategy-specific settings
            _strategies[i].DrawSettings();

            EditorGUILayout.EndVertical();
            GUILayout.Space( 5 );
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        if( GUILayout.Button( "Find Duplicates", GUILayout.Height( 30 ) ) )
        {
            FindDuplicates();
        }

        if( GUILayout.Button( "Apply Filter", GUILayout.Height( 30 ) ) )
        {
            ApplyFilter();
        }

        if( GUILayout.Button( "Clear Filter", GUILayout.Height( 30 ) ) )
        {
            ClearFilter();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space( 10 );

        // Duplicates Draw
        EditorGUILayout.BeginVertical( GUI.skin.box );
        GUILayout.Label( $"Found {_duplicateGroups.Count} duplicate groups:", EditorStyles.boldLabel );

        // Calculate available height for duplicates area
        float availableHeight = position.height - GetAnalysisTabNonDuplicatesHeight();
        availableHeight = Mathf.Max( 100, availableHeight ); // Min height 100px

        _duplicatesScrollPosition = EditorGUILayout.BeginScrollView(
            _duplicatesScrollPosition,
            GUILayout.Height( availableHeight )
        );

        if( _duplicateGroups.Count > 0 )
        {
            for( int i = 0; i < _duplicateGroups.Count; i++ )
            {
                var group = _duplicateGroups[i];

                EditorGUILayout.BeginVertical( EditorStyles.helpBox );

                // Header with toggle
                EditorGUILayout.BeginHorizontal();
                group.IsActive = EditorGUILayout.Toggle( group.IsActive, GUILayout.Width( 20 ) );
                EditorGUILayout.LabelField( $"Group {i + 1} ({group.Sentences.Count} sentences):",
                                            EditorStyles.boldLabel );

                EditorGUILayout.EndHorizontal();

                if( group.IsActive )
                {
                    // Original sentence (orange)
                    var originalStyle = new GUIStyle( EditorStyles.label );
                    originalStyle.normal.textColor = new Color( 1f, 0.5f, 0f );

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField( "Original: ", GUILayout.Width( 60 ) );

                    if( GUILayout.Button( group.OriginalSentence,
                                          originalStyle,
                                          GUILayout.Height( EditorGUIUtility.singleLineHeight * 2 ) ) )
                    {
                        // Reset to default original
                        group.SelectedOriginalIndex = 0;
                        UpdateGroupOriginal( group );
                    }

                    EditorGUILayout.EndHorizontal();

                    // Duplicates (purple)
                    var duplicateStyle = new GUIStyle( EditorStyles.label );
                    duplicateStyle.normal.textColor = new Color( 0.5f, 0f, 0.5f );

                    for( int j = 0; j < group.Duplicates.Count; j++ )
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField( "Duplicate: ", GUILayout.Width( 60 ) );

                        if( GUILayout.Button( group.Duplicates[j],
                                              duplicateStyle,
                                              GUILayout.Height( EditorGUIUtility.singleLineHeight * 2 ) ) )
                        {
                            // Set this duplicate as new original
                            group.SelectedOriginalIndex = j + 1;
                            UpdateGroupOriginal( group );
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox( "Group is disabled - will be ignored in export", MessageType.Info );
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


    private void DrawExportTab()
    {
        GUILayout.Label( "Export Settings", EditorStyles.boldLabel );

        // Export Settings Section with foldout
        EditorGUILayout.BeginVertical( GUI.skin.box );
        {
            EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
            _showExportSection =
                EditorGUILayout.Foldout( _showExportSection, "Export Configuration", true, EditorStyles.foldout );

            EditorGUILayout.EndHorizontal();

            if( _showExportSection )
            {
                EditorGUI.DrawRect( EditorGUILayout.BeginVertical(), new Color( 0.2f, 0.3f, 0.4f, 0.1f ) );
                {
                    // Export tag input
                    _exportTag = EditorGUILayout.TextField( "Export Tag Name", _exportTag );
                    EditorGUILayout.HelpBox( "Enter the XML tag name to use for exported sentences.",
                                             MessageType.Info );
                }

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space( 10 );


        // File path selection
        EditorGUILayout.BeginHorizontal();
        _exportPath = EditorGUILayout.TextField( "Export Path", _exportPath );

        if( GUILayout.Button( "Browse", GUILayout.Width( 60 ) ) )
        {
            _exportPath =
                EditorUtility.SaveFilePanel( "Save XML file", "", "sentences_cleaned.xml", "xml" );
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space( 10 );


        // Statistics and action buttons
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label( $"Original: {_sentences.Count}", EditorStyles.miniLabel, GUILayout.Width( 80 ) );
        GUILayout.Label( $"Filtered: {_filteredSentences.Count}", EditorStyles.miniLabel, GUILayout.Width( 80 ) );
        GUILayout.Label( $"Removed: {_sentences.Count - _filteredSentences.Count}",
                         EditorStyles.miniLabel,
                         GUILayout.Width( 80 ) );

        EditorGUILayout.EndHorizontal();

        GUILayout.Space( 5 );

        EditorGUILayout.BeginHorizontal();
        if( GUILayout.Button( "Export Cleaned XML", GUILayout.Height( 30 ) ) )
        {
            XmlHelper.ExportToXML( _exportPath, _filteredSentences, _exportTag );
        }

        EditorGUILayout.EndHorizontal();
    }


    private void DrawSentencesList( List<string> sentencesList, float height )
    {
        if( sentencesList.Count == 0 )
        {
            EditorGUILayout.HelpBox( "No sentences to display.", MessageType.Info );
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView( _scrollPosition, GUILayout.Height( height ) );

        for( int i = 0; i < sentencesList.Count; i++ )
        {
            EditorGUILayout.LabelField( $"{i + 1}. {sentencesList[i]}",
                                        EditorStyles.textArea,
                                        GUILayout.Height( EditorGUIUtility.singleLineHeight * 2 ) );
        }

        EditorGUILayout.EndScrollView();
    }
#endregion


#region Private Methods
    private void FindDuplicates()
    {
        if( _sentences.Count == 0 )
        {
            EditorUtility.DisplayDialog( "Info", "No sentences to analyze", "OK" );
            return;
        }

        if( _strategies.Count == 0 )
        {
            EditorUtility.DisplayDialog( "Info", "No strategies selected", "OK" );
            return;
        }

        try
        {
            // Reset previous results
            _duplicateGroups.Clear();
            _filteredSentences = new List<string>( _sentences );

            // Apply strategies sequentially
            foreach( var strategy in _strategies.Where( s => s.IsEnabled ) )
            {
                var result = strategy.FindDuplicates( _filteredSentences );
                _filteredSentences = result.RemainingSentences;

                // Add found duplicate groups
                foreach( var group in result.DuplicateGroups )
                {
                    _duplicateGroups.Add( group );
                }
            }

            EditorUtility.DisplayDialog( "Success", $"Found {_duplicateGroups.Count} duplicate groups", "OK" );
        }
        catch( Exception e )
        {
            EditorUtility.DisplayDialog( "Error", $"Failed to find duplicates: {e.Message}", "OK" );
        }
    }


    // Method to calculate the height of all UI elements except the sentences list
    private float GetImportTabNonListHeight()
    {
        // Base height (header, paddings)
        float height = EditorGUIUtility.singleLineHeight * 3; // "Import Settings" + paddings

        // File section height
        height += EditorGUIUtility.singleLineHeight; // Section header
        if( _showFileSection )
            height += EditorGUIUtility.singleLineHeight * 2; // Path field + button

        // Encoding section height
        height += EditorGUIUtility.singleLineHeight; // Section header
        if( _showEncodingSection )
            height += EditorGUIUtility.singleLineHeight * 2; // Encoding selection field

        // Preview section height
        height += EditorGUIUtility.singleLineHeight; // Section header
        if( _showPreviewSection )
            height += 200 + EditorGUIUtility.singleLineHeight * 2; // 200px for scroll area + helpbox

        // Filter section height
        height += EditorGUIUtility.singleLineHeight; // Section header
        if( _showFilterSection )
            height += EditorGUIUtility.singleLineHeight * 3; // Поле фильтра + helpbox

        // Import button and padding height
        height += EditorGUIUtility.singleLineHeight * 4; // Button + paddings

        // Sentences list header height
        height += EditorGUIUtility.singleLineHeight * 3; // Header + toggle + paddings

        height += 14;

        return height;
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


    // Helper method to calculate the height of all elements except the duplicates area
    private float GetAnalysisTabNonDuplicatesHeight()
    {
        // Approximate height of elements (may require precise tuning)
        float height = 100; // Base height (margins, header)

        // Height of buttons
        height += EditorGUIUtility.singleLineHeight * 3;

        // Height of strategies area
        float strategiesHeight = 0;
        foreach( var strategy in _strategies )
        {
            strategiesHeight += EditorGUIUtility.singleLineHeight * 6.5f; // Approximate height per strategy
        }

        height += strategiesHeight;

        // Height of "Find Duplicates" button and margins
        height += EditorGUIUtility.singleLineHeight * 3;

        return height;
    }


    // Helper method to update group original based on selected index
    private void UpdateGroupOriginal( DuplicateGroup group )
    {
        if( group.SelectedOriginalIndex == 0 )
        {
            // Already using default original
            return;
        }

        // Swap original and duplicate
        var newOriginal = group.Duplicates[group.SelectedOriginalIndex - 1];
        group.Duplicates[group.SelectedOriginalIndex - 1] = group.OriginalSentence;
        group.OriginalSentence = newOriginal;
        group.SelectedOriginalIndex = 0;
    }


    private void ApplyFilter()
    {
        try
        {
            var sentencesToKeep = new List<string>();
            var sentencesToRemove = new HashSet<string>();

            // Add all non-duplicate sentences
            sentencesToKeep.AddRange(_filteredSentences);

            // Process duplicate groups
            foreach (var group in _duplicateGroups)
            {
                if (group.IsActive)
                {
                    // Keep only the selected original
                    sentencesToKeep.Add(group.OriginalSentence);
                    
                    // Mark all duplicates for removal
                    foreach (var duplicate in group.Duplicates)
                    {
                        sentencesToRemove.Add(duplicate);
                    }
                }
                else
                {
                    // Keep all sentences from inactive groups
                    sentencesToKeep.AddRange(group.Sentences);
                }
            }

            // Remove duplicates from sentences to keep
            _filteredSentences = sentencesToKeep.Where(s => !sentencesToRemove.Contains(s)).ToList();
            _sentences = new List<string>( _filteredSentences );
            EditorUtility.DisplayDialog("Success", 
                                        $"Applied filter. Now have {_filteredSentences.Count} sentences", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", 
                                        $"Failed to apply filter: {ex.Message}", "OK");
        }
    }


    private void ClearFilter()
    {
        _filteredSentences = new List<string>( _sentences );
        _duplicateGroups.Clear();
        EditorUtility.DisplayDialog( "Info", "Filter cleared. Showing all imported sentences", "OK" );
    }
#endregion
}
