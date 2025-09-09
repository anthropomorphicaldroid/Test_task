using System.Collections.Generic;
using System.Linq;


namespace DuplicateFinder.Data
{
    public class DuplicateGroup
    {
        public string OriginalSentence;
        public List<string> Duplicates = new List<string>();
        public List<string> Sentences
        {
            get => new List<string> {OriginalSentence}.Concat( Duplicates ).ToList();
        }
    }
}
