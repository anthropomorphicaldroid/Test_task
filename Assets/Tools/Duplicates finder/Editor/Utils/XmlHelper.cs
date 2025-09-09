using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Encoder = Tools.EncodingHelper.Encoder;

namespace DuplicateFinder.Utilities
{
    public static class XmlHelper
    {
        // Import XML content with specified encoding and filter tags
        public static List<string> ImportFromXml(string path, string[] tagsToFilter, Encoding encoding)
        {
            List<string> sentences = new List<string>();
            
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return sentences;

            try
            {
                string xmlContent = encoding == null ? 
                    Encoder.ReadAllTextEncodingAuto(path) : 
                    Encoder.ReadAllText(path, encoding);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                if (tagsToFilter == null || tagsToFilter.Length == 0)
                {
                    ExtractAllTextNodes(doc.DocumentElement, sentences);
                }
                else
                {
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
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to import XML: {e.Message}");
            }

            return sentences;
        }

        // Extract all text nodes recursively
        private static void ExtractAllTextNodes(XmlNode node, List<string> sentences)
        {
            if (node.NodeType == XmlNodeType.Text && !string.IsNullOrEmpty(node.Value))
            {
                sentences.Add(node.Value.Trim());
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                ExtractAllTextNodes(child, sentences);
            }
        }

        // Get XML structure as formatted string
        public static string GetXmlStructureExample(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return "Invalid file path";

            try
            {
                string xmlContent = Encoder.ReadAllTextEncodingAuto(path);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                return GetNodeStructureExample(doc.DocumentElement, 0, new HashSet<string>());
            }
            catch (System.Exception e)
            {
                return $"Error reading XML structure: {e.Message}";
            }
        }

        // Recursively generate XML structure description
        private static string GetNodeStructureExample(XmlNode node, int indentLevel, HashSet<string> processedTags)
        {
            string indent = new string(' ', indentLevel * 2);
            string result = indent + "<" + node.Name;

            if (node.Attributes != null && node.Attributes.Count > 0)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    result += " " + attr.Name + "=\"" + attr.Value + "\"";
                }
            }

            result += ">";

            bool hasTextContent = false;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Text && !string.IsNullOrWhiteSpace(child.Value))
                {
                    result += " " + child.Value.Trim();
                    hasTextContent = true;
                    break;
                }
            }

            if (!hasTextContent)
                result += "\n";

            HashSet<string> childTags = new HashSet<string>();

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    if (childTags.Contains(child.Name))
                        continue;

                    childTags.Add(child.Name);
                    result += GetNodeStructureExample(child, indentLevel + 1, processedTags);
                }
            }

            if (!hasTextContent)
            {
                result += indent + "</" + node.Name + ">\n";
            }
            else
            {
                result += "</" + node.Name + ">\n";
            }

            return result;
        }

        // Export sentences to XML file
        public static void ExportToXml(string path, List<string> sentences)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                SentenceData data = new SentenceData();
                data.sentences = sentences;

                System.Xml.Serialization.XmlSerializer serializer = 
                    new System.Xml.Serialization.XmlSerializer(typeof(SentenceData));
                
                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    serializer.Serialize(stream, data);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to export XML: {e.Message}");
            }
        }
    }

    [System.Xml.Serialization.XmlRoot("Sentences")]
    public class SentenceData
    {
        [System.Xml.Serialization.XmlElement("Sentence")]
        public List<string> sentences = new List<string>();
    }
}