using System;
using System.Collections.Generic;
using System.Linq;
using DuplicateFinder.Data;
using UnityEditor;


namespace DuplicateFinder.Strategies
{
    [System.Serializable]
    public class ExactMatchStrategy : ComparisonStrategyBase
    {
        public override string Name => "Exact Match";


        public override void DrawSettings()
        {
            EditorGUILayout.HelpBox( "Finds exact duplicates (case-sensitive).", MessageType.Info );
        }


        public override AnalysisResult FindDuplicates( List<string> sentences )
        {
            var result = new AnalysisResult
            {
                RemainingSentences = new List<string>(), DuplicateGroups = new List<DuplicateGroup>()
            };

            var seen = new HashSet<string>();
            var duplicates = new Dictionary<string, List<string>>();

            foreach( var sentence in sentences )
            {
                if( seen.Contains( sentence ) )
                {
                    // Находим оригинал для этого дубликата
                    var original = seen.First( s => s == sentence );
                    if( !duplicates.ContainsKey( original ) )
                    {
                        duplicates[original] = new List<string>();
                    }

                    duplicates[original].Add( sentence );
                }
                else
                {
                    seen.Add( sentence );
                    result.RemainingSentences.Add( sentence );
                }
            }

            // Создаем группы дубликатов
            foreach( var kvp in duplicates )
            {
                result.DuplicateGroups.Add( new DuplicateGroup {OriginalSentence = kvp.Key, Duplicates = kvp.Value} );
            }

            return result;
        }
    }
}
