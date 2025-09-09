using System;
using System.Collections.Generic;
using DuplicateFinder.Data;
using UnityEditor;


namespace DuplicateFinder.Strategies
{
    [System.Serializable]
    public class LevenshteinStrategy : ComparisonStrategyBase
    {
        public float Threshold = 0.8f;
        public bool CaseSensitive = false;

        public override string Name => "Levenshtein Distance";


        public override void DrawSettings()
        {
            Threshold = EditorGUILayout.Slider( "Similarity Threshold", Threshold, 0.1f, 1.0f );
            CaseSensitive = EditorGUILayout.Toggle( "Case Sensitive", CaseSensitive );
            EditorGUILayout.HelpBox( "Finds similar strings based on edit distance - the minimum number of edits (insertions, deletions, substitutions) to transform one string into another.\n"
                                     + "Detecting typos, fuzzy matching.", MessageType.Info );
        }


        public override AnalysisResult FindDuplicates( List<string> sentences )
        {
            var result = new AnalysisResult
            {
                RemainingSentences = new List<string>(), DuplicateGroups = new List<DuplicateGroup>()
            };

            if( sentences.Count <= 1 )
            {
                result.RemainingSentences = sentences;
                return result;
            }

            var markedForRemoval = new HashSet<int>();
            var duplicateGroups = new Dictionary<int, DuplicateGroup>();

            for( int i = 0; i < sentences.Count; i++ )
            {
                if( markedForRemoval.Contains( i ) )
                    continue;

                result.RemainingSentences.Add( sentences[i] );

                for( int j = i + 1; j < sentences.Count; j++ )
                {
                    if( markedForRemoval.Contains( j ) )
                        continue;

                    string a = CaseSensitive ? sentences[i] : sentences[i].ToLower();
                    string b = CaseSensitive ? sentences[j] : sentences[j].ToLower();

                    float similarity = CalculateLevenshteinSimilarity( a, b );

                    if( similarity >= Threshold )
                    {
                        markedForRemoval.Add( j );

                        if( !duplicateGroups.ContainsKey( i ) )
                        {
                            duplicateGroups[i] = new DuplicateGroup {OriginalSentence = sentences[i]};
                        }

                        duplicateGroups[i].Duplicates.Add( sentences[j] );
                    }
                }
            }

            // Добавляем группы дубликатов в результат
            result.DuplicateGroups.AddRange( duplicateGroups.Values );

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
}
