using System;
using System.Collections.Generic;
using System.Linq;
using DuplicateFinder.Data;
using UnityEditor;


namespace DuplicateFinder.Strategies
{
    [System.Serializable]
    public class JaccardStrategy : ComparisonStrategyBase
    {
        public float Threshold = 0.7f;
        public int NgramSize = 2;
        public bool UseWords = true;

        public override string Name => "Jaccard Similarity";


        public override void DrawSettings()
        {
            Threshold = EditorGUILayout.Slider( "Similarity Threshold", Threshold, 0.1f, 1.0f );
            UseWords = EditorGUILayout.Toggle( "Use Words (instead of n-grams)", UseWords );

            if( !UseWords )
            {
                NgramSize = EditorGUILayout.IntSlider( "N-Gram Size", NgramSize, 1, 5 );
            }

            EditorGUILayout.HelpBox( "Finds similarity based on the ratio of shared Tokens or N-grams to the total unique set.\n"
                                     + "Detecting duplicate or near-duplicate texts, plagiarism detection.", MessageType.Info );
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

            // Предварительно вычисляем множества для каждого предложения
            List<HashSet<string>> sets = new List<HashSet<string>>();
            foreach( var sentence in sentences )
            {
                sets.Add( UseWords ? CreateWordSet( sentence ) : CreateNGramSet( sentence, NgramSize ) );
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

                    float similarity = CalculateJaccardSimilarity( sets[i], sets[j] );

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
}
