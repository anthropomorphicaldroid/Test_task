using System.Collections.Generic;
using DuplicateFinder.Data;


namespace DuplicateFinder.Strategies
{
    public abstract class ComparisonStrategyBase
    {
        public bool IsEnabled = true;
        public abstract string Name { get; }
        public abstract void DrawSettings();
        public abstract AnalysisResult FindDuplicates( List<string> sentences );
    }
}
