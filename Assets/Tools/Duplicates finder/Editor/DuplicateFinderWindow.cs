using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Xml; // Добавляем для работы с XmlDocument

public class DuplicateFinderWindow : EditorWindow
{
    [SerializeField] private List<string> sentences = new List<string>();
    [SerializeField] private List<string> filteredSentences = new List<string>();
    [SerializeField] private List<bool> sentenceSelection = new List<bool>();
    
    private Vector2 scrollPosition;
    private string importPath = "";
    private string exportPath = "";
    private float similarityThreshold = 0.8f;
    private int selectedTab = 0;
    private string[] tabNames = { "Import", "Analysis", "Manual Review", "Export" };
    
    // Новые поля для отображения структуры XML и фильтрации
    private string xmlStructure = "";
    private string filterTags = "";
    private Vector2 structureScrollPosition;
    
    [MenuItem("Tools/Duplicate Finder")]
    public static void ShowWindow()
    {
        GetWindow<DuplicateFinderWindow>("Duplicate Finder");
    }

    private void OnGUI()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        
        switch (selectedTab)
        {
            case 0:
                DrawImportTab();
                break;
            case 1:
                DrawAnalysisTab();
                break;
            case 2:
                DrawManualReviewTab();
                break;
            case 3:
                DrawExportTab();
                break;
        }
    }

    private void DrawImportTab()
    {
        GUILayout.Label("Import Settings", EditorStyles.boldLabel);
    
        EditorGUILayout.BeginHorizontal();
        importPath = EditorGUILayout.TextField("XML Path", importPath);
    
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string newPath = EditorUtility.OpenFilePanel("Select XML file", "", "xml");
            if (!string.IsNullOrEmpty(newPath))
            {
                importPath = newPath;
                xmlStructure = GetXmlTagsStructure(importPath);
            }
        }
        EditorGUILayout.EndHorizontal();
    
        // Отображение уникальных тегов XML
        if (!string.IsNullOrEmpty(xmlStructure))
        {
            GUILayout.Label("Available XML Tags:", EditorStyles.boldLabel);
            structureScrollPosition = EditorGUILayout.BeginScrollView(structureScrollPosition, GUILayout.Height(100));
            EditorGUILayout.TextArea(xmlStructure, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        
            EditorGUILayout.HelpBox("Copy tags from above and paste in the field below to filter content.", MessageType.Info);
        }
    
        // Поле для ввода тегов фильтрации
        filterTags = EditorGUILayout.TextField("Filter Tags (comma separated)", filterTags);
    
        if (GUILayout.Button("Import XML"))
        {
            string[] tags = filterTags.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tags.Length; i++)
            {
                tags[i] = tags[i].Trim();
            }
        
            ImportFromXML(importPath, tags);
        }
    
        GUILayout.Space(10);
        GUILayout.Label($"Imported Sentences: {sentences.Count}", EditorStyles.boldLabel);
        DrawSentencesList(sentences);
    }



    private void DrawAnalysisTab()
    {
        GUILayout.Label("Analysis Settings", EditorStyles.boldLabel);
        
        similarityThreshold = EditorGUILayout.Slider("Similarity Threshold", 
            similarityThreshold, 0.1f, 1.0f);
        
        if (GUILayout.Button("Find Duplicates"))
        {
            FindDuplicates();
        }
        
        GUILayout.Space(10);
        GUILayout.Label($"Filtered Sentences: {filteredSentences.Count}", EditorStyles.boldLabel);
        DrawSentencesList(filteredSentences);
    }

    private void DrawExportTab()
    {
        GUILayout.Label("Export Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            exportPath = EditorUtility.SaveFilePanel("Save XML file", "", "sentences_cleaned.xml", "xml");
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Export Cleaned XML"))
        {
            ExportToXML(exportPath, filteredSentences);
        }
        
        if (GUILayout.Button("Export Original XML"))
        {
            ExportToXML(exportPath, sentences);
        }
    }

    private void DrawManualReviewTab()
    {
        GUILayout.Label("Manual Review", EditorStyles.boldLabel);
        GUILayout.Label("Select sentences to keep (uncheck duplicates):", EditorStyles.helpBox);
        
        if (sentenceSelection.Count != sentences.Count)
        {
            sentenceSelection = sentences.Select(s => true).ToList();
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < sentences.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sentenceSelection[i] = EditorGUILayout.Toggle(sentenceSelection[i], GUILayout.Width(20));
            EditorGUILayout.LabelField($"{i + 1}. {sentences[i]}", 
                EditorStyles.textArea, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
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
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, 
            GUILayout.Height(300));
        
        for (int i = 0; i < sentencesList.Count; i++)
        {
            EditorGUILayout.LabelField($"{i + 1}. {sentencesList[i]}", 
                EditorStyles.textArea, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
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
            sentences.Clear();
            
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            
            // Если теги для фильтрации не указаны, извлекаем все текстовые узлы
            if (tagsToFilter == null || tagsToFilter.Length == 0)
            {
                ExtractAllTextNodes(doc.DocumentElement);
            }
            else
            {
                // Извлекаем только узлы с указанными тегами
                foreach (string tag in tagsToFilter)
                {
                    XmlNodeList nodes = doc.GetElementsByTagName(tag);
                    foreach (XmlNode node in nodes)
                    {
                        if (!string.IsNullOrEmpty(node.InnerText))
                            sentences.Add(node.InnerText.Trim());
                    }
                }
            }
            
            filteredSentences = new List<string>(sentences);
            EditorUtility.DisplayDialog("Success", 
                $"Imported {sentences.Count} sentences", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", 
                $"Failed to import XML: {e.Message}", "OK");
        }
    }

    // Новый метод для извлечения всех текстовых узлов
    private void ExtractAllTextNodes(XmlNode node)
    {
        if (node.NodeType == XmlNodeType.Text && !string.IsNullOrEmpty(node.Value))
        {
            sentences.Add(node.Value.Trim());
        }
        
        foreach (XmlNode child in node.ChildNodes)
        {
            ExtractAllTextNodes(child);
        }
    }

    private string GetXmlTagsStructure(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return "Invalid file path";

        try
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
        
            HashSet<string> uniqueTags = new HashSet<string>();
            CollectUniqueTags(doc.DocumentElement, uniqueTags);
        
            // Сортируем теги для удобства чтения
            List<string> sortedTags = new List<string>(uniqueTags);
            sortedTags.Sort();
        
            return "Available tags:\n" + string.Join(", ", sortedTags);
        }
        catch (System.Exception e)
        {
            return $"Error reading XML structure: {e.Message}";
        }
    }

// Метод для сбора уникальных тегов
    private void CollectUniqueTags(XmlNode node, HashSet<string> uniqueTags)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            uniqueTags.Add(node.Name);
        }
    
        foreach (XmlNode child in node.ChildNodes)
        {
            CollectUniqueTags(child, uniqueTags);
        }
    }


    // Новый метод для рекурсивного получения структуры узлов
    private string GetNodeStructure(XmlNode node, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);
        string result = indent + "<" + node.Name + ">\n";
        
        // Добавляем атрибуты, если они есть
        if (node.Attributes != null && node.Attributes.Count > 0)
        {
            foreach (XmlAttribute attr in node.Attributes)
            {
                result += indent + "  " + attr.Name + "=\"" + attr.Value + "\"\n";
            }
        }
        
        // Рекурсивно обрабатываем дочерние элементы
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                result += GetNodeStructure(child, indentLevel + 1);
            }
            else if (child.NodeType == XmlNodeType.Text)
            {
                result += indent + "  " + "Text: " + child.Value + "\n";
            }
        }
        
        return result;
    }


    private void ExportToXML(string path, List<string> dataToExport)
    {
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Error", "Invalid file path", "OK");
            return;
        }

        try
        {
            SentenceData data = new SentenceData();
            data.sentences = dataToExport;
            
            XmlSerializer serializer = new XmlSerializer(typeof(SentenceData));
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(stream, data);
                EditorUtility.DisplayDialog("Success", 
                    $"Exported {dataToExport.Count} sentences", "OK");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", 
                $"Failed to export XML: {e.Message}", "OK");
        }
    }

    private void FindDuplicates()
    {
        // Заглушка для алгоритма поиска дубликатов
        // В реальной реализации здесь будет сложная логика сравнения
        filteredSentences = sentences.Distinct().ToList();
        EditorUtility.DisplayDialog("Info", 
            $"Found {sentences.Count - filteredSentences.Count} duplicates", "OK");
    }

    private void ApplyManualSelection()
    {
        filteredSentences = new List<string>();
        
        for (int i = 0; i < sentences.Count; i++)
        {
            if (sentenceSelection[i])
            {
                filteredSentences.Add(sentences[i]);
            }
        }
        
        EditorUtility.DisplayDialog("Success", 
            $"Filtered to {filteredSentences.Count} sentences", "OK");
    }

    [XmlRoot("Sentences")]
    public class SentenceData
    {
        [XmlElement("Sentence")] 
        public List<string> sentences = new List<string>();
    }
}