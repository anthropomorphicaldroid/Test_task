using System.Collections.Generic;

namespace DuplicateFinder.Data
{
    public class AnalysisResult
    {
        public List<string> RemainingSentences;
        public List<DuplicateGroup> DuplicateGroups;

        public AnalysisResult()
        {
            RemainingSentences = new List<string>();
            DuplicateGroups = new List<DuplicateGroup>();
        }
    }
}
