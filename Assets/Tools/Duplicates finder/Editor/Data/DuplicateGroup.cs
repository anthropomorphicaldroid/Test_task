using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace DuplicateFinder.Data
{
    [Serializable]
    public class DuplicateGroup
    {
        public string OriginalSentence;
        public List<string> Duplicates = new List<string>();
    
        [SerializeField]
        private bool _isActive = true;
        [SerializeField]
        private int _selectedOriginalIndex = 0;

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public int SelectedOriginalIndex
        {
            get => _selectedOriginalIndex;
            set => _selectedOriginalIndex = value;
        }

        public List<string> Sentences => new List<string> { OriginalSentence }.Concat(Duplicates).ToList();
    }
}
