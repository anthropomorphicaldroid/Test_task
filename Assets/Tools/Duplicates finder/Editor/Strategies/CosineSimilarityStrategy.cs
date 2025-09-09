using System;
using System.Collections.Generic;
using System.Linq;
using DuplicateFinder.Data;
using UnityEditor;


namespace DuplicateFinder.Strategies
{
    [System.Serializable]
    public class CosineSimilarityStrategy : ComparisonStrategyBase
    {
        public float threshold = 0.75f;
        public bool useTfIdf = true;

        public override string Name => "Cosine Similarity";


        public override void DrawSettings()
        {
            threshold = EditorGUILayout.Slider( "Similarity Threshold", threshold, 0.1f, 1.0f );
            useTfIdf = EditorGUILayout.Toggle( "Use TF-IDF", useTfIdf );
            EditorGUILayout.HelpBox( "Compares strings as vectors of word frequencies and measures how close their directions are.\n"
                                     + "Detecting semantically similar sentences even if the wording differs.\n"
                                     + "TF-IDF assigns higher weight to words that are frequent in a given string but rare across the whole dataset, helping highlight the most informative terms.", MessageType.Info );
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

                result.RemainingSentences.Add( sentences[i] );

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
}
