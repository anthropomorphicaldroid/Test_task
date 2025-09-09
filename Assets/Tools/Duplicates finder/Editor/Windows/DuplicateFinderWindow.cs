using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DuplicateFinder.Utilities;
using DuplicateFinder.Strategies;
using DuplicateFinder.Data;

public class DuplicateFinderWindow : EditorWindow
{
    // Serialized fields
    [SerializeField] private List<string> Sentences = new List<string>();
    [SerializeField] private List<string> FilteredSentences = new List<string>();
    [SerializeField] private List<bool> SentenceSelection = new List<bool>();
    [SerializeField] private List<ComparisonStrategyBase> strategies = new List<ComparisonStrategyBase>();
    [SerializeField] private List<DuplicateGroup> duplicateGroups = new List<DuplicateGroup>();

    // UI state variables
    private Vector2 _scrollPosition;
    private Vector2 _strategiesScrollPosition = Vector2.zero;
    private Vector2 _duplicatesScrollPosition = Vector2.zero;
    private string _importPath = "";
    private string _exportPath = "";
    private readonly string[] _tabNames = { "Import", "Analysis", "Manual Review", "Export" };
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

    // Encoding options
    private readonly string[] _encodingOptions = {
        "Auto", "UTF-8", "Windows-1251", "Windows-1252", "Unicode", "BigEndianUnicode"
    };

    [MenuItem("Tools/Duplicate Finder")]
    public static void ShowWindow()
    {
        GetWindow<DuplicateFinderWindow>("Duplicate Finder");
    }

    private void OnEnable()
    {
        // Load UI state from PlayerPrefs
        _showFileSection = PlayerPrefs.GetInt("DuplicateFinder.showFileSection") == 1;
        _showEncodingSection = PlayerPrefs.GetInt("DuplicateFinder.showEncodingSection") == 1;
        _showFilterSection = PlayerPrefs.GetInt("DuplicateFinder.showFilterSection") == 1;
        _showPreviewSection = PlayerPrefs.GetInt("DuplicateFinder.showPreviewSection") == 1;
        _isDrawSentences = PlayerPrefs.GetInt("DuplicateFinder.isDrawSentences") == 1;
    }

    private void OnDisable()
    {
        // Save UI state to PlayerPrefs
        PlayerPrefs.SetInt("DuplicateFinder.showFileSection", _showFileSection ? 1 : 0);
        PlayerPrefs.SetInt("DuplicateFinder.showEncodingSection", _showEncodingSection ? 1 : 0);
        PlayerPrefs.SetInt("DuplicateFinder.showFilterSection", _showFilterSection ? 1 : 0);
        PlayerPrefs.SetInt("DuplicateFinder.showPreviewSection", _showPreviewSection ? 1 : 0);
        PlayerPrefs.SetInt("DuplicateFinder.isDrawSentences", _isDrawSentences ? 1 : 0);
    }

    private void OnGUI()
    {
        _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

        switch (_selectedTab)
        {
            case 0: DrawImportTab(); break;
            case 1: DrawAnalysisTab(); break;
            case 2: DrawManualReviewTab(); break;
            case 3: DrawExportTab(); break;
        }
    }

    private void DrawImportTab()
    {
        GUILayout.Label("Import Settings", EditorStyles.boldLabel);

        // File Selection Section
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _showFileSection = EditorGUILayout.Foldout(_showFileSection, "File Selection", true, EditorStyles.foldout);
            EditorGUILayout.EndHorizontal();

            if (_showFileSection)
            {
                EditorGUI.DrawRect(EditorGUILayout.BeginVertical(), new Color(0.2f, 0.2f, 0.2f, 0.1f));
                {
                    EditorGUILayout.BeginHorizontal();
                    _importPath = EditorGUILayout.TextField("XML Path", _importPath);

                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        string newPath = EditorUtility.OpenFilePanel("Select XML file", "", "xml");
                        if (!string.IsNullOrEmpty(newPath))
                        {
                            _importPath = newPath;
                            _xmlStructure = XmlHelper.GetXmlStructureExample(_importPath);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Encoding Selection Section
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _showEncodingSection = EditorGUILayout.Foldout(_showEncodingSection, "Encoding Settings", true, EditorStyles.foldout);
            EditorGUILayout.EndHorizontal();

            if (_showEncodingSection)
            {
                EditorGUI.DrawRect(EditorGUILayout.BeginVertical(), new Color(0.2f, 0.3f, 0.2f, 0.1f));
                {
                    GUILayout.Label("File Encoding:");
                    int newEncodingIndex = EditorGUILayout.Popup(_selectedEncodingIndex, _encodingOptions);
                    if (newEncodingIndex != _selectedEncodingIndex)
                    {
                        _selectedEncodingIndex = newEncodingIndex;
                        _selectedEncoding = GetEncodingFromIndex(_selectedEncodingIndex);
                    }
                    else
                    {
                        _selectedEncoding = GetEncodingFromIndex(_selectedEncodingIndex);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // XML Structure Preview Section
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _showPreviewSection = EditorGUILayout.Foldout(_showPreviewSection, "XML Structure Preview", true, EditorStyles.foldout);
            EditorGUILayout.EndHorizontal();

            if (_showPreviewSection)
            {
                EditorGUI.DrawRect(EditorGUILayout.BeginVertical(), new Color(0.3f, 0.2f, 0.2f, 0.1f));
                {
                    _structureScrollPosition = EditorGUILayout.BeginScrollView(_structureScrollPosition, GUILayout.Height(200));
                    EditorGUILayout.TextArea(_xmlStructure, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.HelpBox(
                        "This shows the structure with one example of each tag. Use these tag names for filtering above.",
                        MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Filter Section
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _showFilterSection = EditorGUILayout.Foldout(_showFilterSection, "Filter Settings", true, EditorStyles.foldout);
            EditorGUILayout.EndHorizontal();

            if (_showFilterSection)
            {
                EditorGUI.DrawRect(EditorGUILayout.BeginVertical(), new Color(0.2f, 0.2f, 0.3f, 0.1f));
                {
                    _filterTags = EditorGUILayout.TextField("Filter Tags (comma separated)", _filterTags);
                    EditorGUILayout.HelpBox("Enter tag names separated by commas to extract specific content",
                                             MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        if (GUILayout.Button("Import XML", GUILayout.Height(30)))
        {
            string[] tags = _filterTags.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tags.Length; i++)
            {
                tags[i] = tags[i].Trim();
            }

            ImportFromXML(_importPath, tags);
        }

        GUILayout.Space(10);

        // Imported Sentences Section
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            GUILayout.Label($"Imported Sentences: {Sentences.Count}", EditorStyles.boldLabel);
            _isDrawSentences = GUILayout.Toggle(_isDrawSentences, "Show lines");
            if (_isDrawSentences)
            {
                DrawSentencesList(Sentences);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAnalysisTab()
    {
        GUILayout.Label("Analysis Strategies", EditorStyles.boldLabel);

        // Strategy selection and management
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Strategy"))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Exact Match"), false, () => strategies.Add(new ExactMatchStrategy()));
            menu.AddItem(new GUIContent("Levenshtein Distance"), false, () => strategies.Add(new LevenshteinStrategy()));
            menu.AddItem(new GUIContent("Jaccard Similarity"), false, () => strategies.Add(new JaccardStrategy()));
            menu.AddItem(new GUIContent("Cosine Similarity"), false, () => strategies.Add(new CosineSimilarityStrategy()));
            menu.ShowAsContext();
        }

        if (GUILayout.Button("Clear All"))
        {
            strategies.Clear();
        }
        EditorGUILayout.EndHorizontal();

        // Strategies display
        EditorGUILayout.BeginVertical(GUI.skin.box);
        _strategiesScrollPosition = EditorGUILayout.BeginScrollView(_strategiesScrollPosition);

        for (int i = 0; i < strategies.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            strategies[i].IsEnabled = EditorGUILayout.Toggle(strategies[i].IsEnabled, GUILayout.Width(20));
            EditorGUILayout.LabelField(strategies[i].Name, EditorStyles.boldLabel);

            if (GUILayout.Button("↑", GUILayout.Width(20)) && i > 0)
            {
                var temp = strategies[i - 1];
                strategies[i - 1] = strategies[i];
                strategies[i] = temp;
            }

            if (GUILayout.Button("↓", GUILayout.Width(20)) && i < strategies.Count - 1)
            {
                var temp = strategies[i + 1];
                strategies[i + 1] = strategies[i];
                strategies[i] = temp;
            }

            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                strategies.RemoveAt(i);
                i--;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }
            EditorGUILayout.EndHorizontal();

            // Display strategy-specific settings
            strategies[i].DrawSettings();

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Find Duplicates"))
        {
            FindDuplicates();
        }

        GUILayout.Space(10);

        // Duplicates display
        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label($"Found {duplicateGroups.Count} duplicate groups:", EditorStyles.boldLabel);

        // Calculate available height for duplicates area
        float availableHeight = position.height - GetAnalysisTabNonDuplicatesHeight();
        availableHeight = Mathf.Max(100, availableHeight); // Min height 100px

        _duplicatesScrollPosition = EditorGUILayout.BeginScrollView(
            _duplicatesScrollPosition,
            GUILayout.Height(availableHeight)
        );

        if (duplicateGroups.Count > 0)
        {
            for (int i = 0; i < duplicateGroups.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"Group {i + 1} ({duplicateGroups[i].Sentences.Count} sentences):",
                                            EditorStyles.boldLabel);

                // Original sentence (orange)
                var originalStyle = new GUIStyle(EditorStyles.label);
                originalStyle.normal.textColor = new Color(1f, 0.6f, 0f);
                EditorGUILayout.LabelField($"{duplicateGroups[i].OriginalSentence}", originalStyle);

                // Duplicates (teal)
                var duplicateStyle = new GUIStyle(EditorStyles.label);
                duplicateStyle.normal.textColor = new Color(0.1f, 0.8f, 0.7f);
                foreach (var duplicate in duplicateGroups[i].Duplicates)
                {
                    EditorGUILayout.LabelField($"{duplicate}", duplicateStyle);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }
        else
        {
            GUILayout.Label("No duplicates found", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawExportTab()
    {
        GUILayout.Label("Export Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _exportPath = EditorGUILayout.TextField("Export Path", _exportPath);

        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            _exportPath = EditorUtility.SaveFilePanel("Save XML file", "", "sentences_cleaned.xml", "xml");
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Export Cleaned XML"))
        {
            XmlHelper.ExportToXml(_exportPath, FilteredSentences);
            EditorUtility.DisplayDialog("Success", $"Exported {FilteredSentences.Count} sentences", "OK");
        }

        if (GUILayout.Button("Export Original XML"))
        {
            XmlHelper.ExportToXml(_exportPath, Sentences);
            EditorUtility.DisplayDialog("Success", $"Exported {Sentences.Count} sentences", "OK");
        }
    }

    private void DrawManualReviewTab()
    {
        GUILayout.Label("Manual Review", EditorStyles.boldLabel);
        GUILayout.Label("Select strings to keep (uncheck duplicates):", EditorStyles.helpBox);

        if (SentenceSelection.Count != Sentences.Count)
        {
            SentenceSelection = Sentences.Select(s => true).ToList();
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        for (int i = 0; i < Sentences.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            SentenceSelection[i] = EditorGUILayout.Toggle(SentenceSelection[i], GUILayout.Width(20));
            EditorGUILayout.LabelField($"{i + 1}. {Sentences[i]}",
                                        EditorStyles.textArea,
                                        GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Apply Selection"))
        {
            ApplyManualSelection();
        }
    }

    private void DrawSentencesList(List<string> sentencesList)
    {
        if (sentencesList.Count == 0)
        {
            EditorGUILayout.HelpBox("No sentences to display.", MessageType.Info);
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));

        for (int i = 0; i < sentencesList.Count; i++)
        {
            EditorGUILayout.LabelField($"{i + 1}. {sentencesList[i]}",
                                        EditorStyles.textArea,
                                        GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
        }

        EditorGUILayout.EndScrollView();
    }

    private void ImportFromXML(string path, string[] tagsToFilter)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            EditorUtility.DisplayDialog("Error", "Invalid file path", "OK");
            return;
        }

        try
        {
            Sentences = XmlHelper.ImportFromXml(path, tagsToFilter, _selectedEncoding);
            FilteredSentences = new List<string>(Sentences);
            EditorUtility.DisplayDialog("Success", $"Imported {Sentences.Count} sentences", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to import XML: {e.Message}", "OK");
        }
    }

    private Encoding GetEncodingFromIndex(int index)
    {
        switch (index)
        {
            case 0: return Encoding.Default;
            case 1: return Encoding.UTF8;
            case 2: return Encoding.GetEncoding(1251);
            case 3: return Encoding.GetEncoding(1252);
            case 4: return Encoding.Unicode;
            case 5: return Encoding.BigEndianUnicode;
            default: return Encoding.UTF8;
        }
    }

    private void FindDuplicates()
    {
        if (Sentences.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No sentences to analyze", "OK");
            return;
        }

        if (strategies.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No strategies selected", "OK");
            return;
        }

        try
        {
            // Reset previous results
            duplicateGroups.Clear();
            FilteredSentences = new List<string>(Sentences);

            // Apply strategies sequentially
            foreach (var strategy in strategies.Where(s => s.IsEnabled))
            {
                var result = strategy.FindDuplicates(FilteredSentences);
                FilteredSentences = result.RemainingSentences;

                // Add found duplicate groups
                foreach (var group in result.DuplicateGroups)
                {
                    duplicateGroups.Add(group);
                }
            }

            EditorUtility.DisplayDialog("Success", $"Found {duplicateGroups.Count} duplicate groups", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to find duplicates: {e.Message}", "OK");
        }
    }

    private void ApplyManualSelection()
    {
        FilteredSentences = new List<string>();

        for (int i = 0; i < Sentences.Count; i++)
        {
            if (SentenceSelection[i])
            {
                FilteredSentences.Add(Sentences[i]);
            }
        }

        EditorUtility.DisplayDialog("Success", $"Filtered to {FilteredSentences.Count} sentences", "OK");
    }

    // Helper method to calculate the height of all elements except the duplicates area
    private float GetAnalysisTabNonDuplicatesHeight()
    {
        // Approximate height of elements (may require precise tuning)
        float height = 100; // Base height (margins, header)

        // Height of buttons
        height += EditorGUIUtility.singleLineHeight * 2;

        // Height of strategies area
        float strategiesHeight = 0;
        foreach (var strategy in strategies)
        {
            strategiesHeight += EditorGUIUtility.singleLineHeight * 4; // Approximate height per strategy
        }
        height += strategiesHeight;

        // Height of "Find Duplicates" button and margins
        height += EditorGUIUtility.singleLineHeight * 3;

        return height;
    }
}