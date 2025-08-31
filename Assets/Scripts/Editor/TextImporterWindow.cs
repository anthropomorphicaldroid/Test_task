using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Tools.Encoding;


public class TextImporterWindow : EditorWindow
{
    private List<string> sentences = new();
    private Vector2 scroll;

    [MenuItem("Tools/Text Importer")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<TextImporterWindow>();
        wnd.titleContent = new GUIContent("Text Importer");
        wnd.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Импорт текстов", EditorStyles.boldLabel);

        GUILayout.Space(5);

        if (GUILayout.Button("Загрузить XML"))
        {
            string path = EditorUtility.OpenFilePanel("Выберите XML файл", "", "xml");
            if (!string.IsNullOrEmpty(path)) LoadFromXml(path);
        }

        if (GUILayout.Button("Загрузить JSON"))
        {
            string path = EditorUtility.OpenFilePanel("Выберите JSON файл", "", "json");
            if (!string.IsNullOrEmpty(path)) LoadFromJson(path);
        }

        if (GUILayout.Button("Загрузить TXT"))
        {
            string path = EditorUtility.OpenFilePanel("Выберите TXT файл", "", "txt");
            if (!string.IsNullOrEmpty(path)) LoadFromTxt(path);
        }

        GUILayout.Space(10);

        GUILayout.Label($"Загружено строк: {sentences.Count}");

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(300));
        for (int i = 0; i < sentences.Count; i++)
        {
            EditorGUILayout.LabelField($"{i + 1}. {sentences[i]}");
        }
        GUILayout.EndScrollView();

        if (sentences.Count > 0)
        {
            GUILayout.Space(10);

            GUILayout.Label("Экспорт:", EditorStyles.boldLabel);

            if (GUILayout.Button("Экспортировать в XML"))
            {
                string path = EditorUtility.SaveFilePanel("Сохранить как XML", "", "strings.xml", "xml");
                if (!string.IsNullOrEmpty(path)) SaveToXml(path);
            }

            if (GUILayout.Button("Экспортировать в JSON"))
            {
                string path = EditorUtility.SaveFilePanel("Сохранить как JSON", "", "strings.json", "json");
                if (!string.IsNullOrEmpty(path)) SaveToJson(path);
            }

            if (GUILayout.Button("Экспортировать в TXT"))
            {
                string path = EditorUtility.SaveFilePanel("Сохранить как TXT", "", "strings.txt", "txt");
                if (!string.IsNullOrEmpty(path)) SaveToTxt(path);
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Очистить список"))
                sentences.Clear();
        }
    }

    // ===== Импорт =====
    private void LoadFromXml(string path)
    {
        try
        {
            string xmlText = EncodingHelper.ReadAllTextAuto(path);
            using (var reader = new StringReader(xmlText))
            {
                var doc = System.Xml.Linq.XDocument.Load(reader);
                sentences = doc.Descendants("string")
                               .Select(x => x.Value.Trim())
                               .Where(x => !string.IsNullOrEmpty(x))
                               .ToList();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка чтения XML: {ex.Message}");
        }
    }


    private void LoadFromJson(string path)
    {
        try
        {
            string json = EncodingHelper.ReadAllTextAuto(path);
            var arr = JsonUtility.FromJson<StringArrayWrapper>(json);
            sentences = arr?.strings?.ToList() ?? new List<string>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка чтения JSON: {ex.Message}");
        }
    }



    private void LoadFromTxt(string path)
    {
        try
        {
            var lines = EncodingHelper.ReadAllLinesAuto(path);
            sentences = lines.Select(l => l.Trim())
                             .Where(l => !string.IsNullOrEmpty(l))
                             .ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка чтения TXT: {ex.Message}");
        }
    }


    // ===== Экспорт =====
    private void SaveToXml(string path)
    {
        try
        {
            var doc = new XDocument(new XElement("root",
                sentences.Select(s => new XElement("string", s))
            ));
            doc.Save(path);
            AssetDatabase.Refresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка сохранения XML: {ex.Message}");
        }
    }

    private void SaveToJson(string path)
    {
        try
        {
            var wrapper = new StringArrayWrapper { strings = sentences.ToArray() };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка сохранения JSON: {ex.Message}");
        }
    }

    private void SaveToTxt(string path)
    {
        try
        {
            File.WriteAllLines(path, sentences, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка сохранения TXT: {ex.Message}");
        }
    }

    [System.Serializable]
    private class StringArrayWrapper
    {
        public string[] strings;
    }
}
