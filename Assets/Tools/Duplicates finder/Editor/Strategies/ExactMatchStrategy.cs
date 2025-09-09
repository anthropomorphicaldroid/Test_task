using System;
using System.Collections.Generic;
using System.Linq;
using DuplicateFinder.Data;
using UnityEditor;
using UnityEngine;


namespace DuplicateFinder.Strategies
{
    [System.Serializable]
    public class ExactMatchStrategy : ComparisonStrategyBase
    {
        [SerializeField]
        private bool CaseSensitive = true;

        public override string Name => "Exact Match";


        public override void DrawSettings()
        {
            CaseSensitive = EditorGUILayout.Toggle( "Case Sensitive", CaseSensitive );
            EditorGUILayout.HelpBox( "Finds exact duplicates with case sensitivity option.", MessageType.Info );
        }


        public override AnalysisResult FindDuplicates( List<string> sentences )
        {
            var result = new AnalysisResult
            {
                RemainingSentences = new List<string>(), DuplicateGroups = new List<DuplicateGroup>()
            };

            var comparer = CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

            var seen = new HashSet<string>( comparer );
            var duplicates = new Dictionary<string, List<string>>( comparer );

            foreach( var sentence in sentences )
            {
                string key = CaseSensitive ? sentence : sentence.ToLowerInvariant();

                if( seen.Contains( key ) )
                {
                    var original = seen.First( s => comparer.Equals( s, key ) );
                    if( !duplicates.ContainsKey( original ) )
                    {
                        duplicates[original] = new List<string>();
                    }

                    duplicates[original].Add( sentence );
                }
                else
                {
                    seen.Add( key );
                    result.RemainingSentences.Add( sentence );
                }
            }

            foreach( var kvp in duplicates )
            {
                // Restore original sentence from key
                string originalSentence = CaseSensitive
                                              ? kvp.Key
                                              : sentences.First( s => comparer.Equals( s.ToLowerInvariant(),
                                                                     kvp.Key ) );

                result.DuplicateGroups.Add( new DuplicateGroup
                {
                    OriginalSentence = originalSentence, Duplicates = kvp.Value
                } );
            }

            return result;
        }
    }
}
