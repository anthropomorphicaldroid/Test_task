using System;
using System.Collections.Generic;
using UnityEngine;


namespace DuplicateFinder.Data
{
    [Serializable]
    public class DuplicateGroup
    {
        public string OriginalSentence;
        public List<string> Duplicates = new List<string>();
        public List<bool> Selection = new List<bool>();

        [SerializeField]
        private bool _isActive = true;

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public List<string> Sentences
        {
            get
            {
                var allSentences = new List<string> {OriginalSentence};
                allSentences.AddRange( Duplicates );
                return allSentences;
            }
        }
    }
}
