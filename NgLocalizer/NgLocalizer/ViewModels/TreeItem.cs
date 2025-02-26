using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NgLocalizer.ViewModels
{
    public class TreeItem
    : ViewModel
    {
        private static readonly Regex KeyRegex = new("^[a-zA-Z0-9-_]+(\\.[a-zA-Z0-9-_]+)*$");
        private string _key;
        private List<Translation> _translations;
        private bool _isExpanded;
        private bool _isSelected;
        private List<TokenUsage> _tokenUsages;
        private TokenUsage _selectedTokenUsage;
        private int _tokenUsageSelectionIndex;
        private Translation _selectedTranslation;

        public TreeItem()
        {
            Children = new List<TreeItem>();
            Translations = null;
        }

        public string Key
        {
            get => _key;
            set
            {
                if (value == _key) return;
                _key = value;
                OnPropertyChanged();
            }
        }

        public void SelectNextTokenUsage()
        {
            if (TokenUsageSelectionIndex < TokenUsages.Count)
                TokenUsageSelectionIndex++;

            SelectedTokenUsage = TokenUsages[TokenUsageSelectionIndex - 1];
        }

        public void SelectPreviousTokenUsage()
        {
            if (TokenUsageSelectionIndex > 1)
                TokenUsageSelectionIndex--;

            SelectedTokenUsage = TokenUsages[TokenUsageSelectionIndex - 1];
        }

        public string Label => IsTextItem && IsValid && IsDefaultKey ? Key[KeyNameGenerator.GetNamespace(this).Length..] : Key;

        public bool IsTextItem => Translations != null;
        public bool HasSourceFile => FullFilename != null;
        public bool IsValid => !IsTextItem || KeyRegex.IsMatch(Key);
        public bool IsFolder { get; set; }
        public TreeItem Parent { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value == _isExpanded) return;
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public List<Translation> Translations
        {
            get => _translations;
            set
            {
                if (value != null)
                {
                    foreach (var translation in value)
                    {
                        translation.PropertyChanged += (sender, args) => NotifyTranslationChanges();
                    }
                }
                _translations = value;
            }
        }

        /// <summary>
        /// Filled in if this TreeItem represents a source file.
        /// </summary>
        public string FullFilename { get; set; }
        /// <summary>
        /// We keep this for Sourcefile TreeItems to only allow changing the source file from this tool if nothing else has changes the file since the last Scan.
        /// </summary>
        public DateTime LastSourceFileWrite { get; set; }

        private void NotifyTranslationChanges()
        {
            OnPropertyChanged(nameof(HasMissingTranslations));
            OnPropertyChanged(nameof(MissingTranslationsCount));
            Parent?.NotifyTranslationChanges();
        }

        public List<TreeItem> Children { get; set; }
        public bool HasChildren => Children.Any();
        public bool HasMissingTranslations => MissingTranslationsCount > 0;
        public int MissingTranslationsCount => (IsTextItem ? Translations.Count(t => string.IsNullOrEmpty(t.Text)) : 0) + Children.Sum(c => c.MissingTranslationsCount);
        public string MissingTranslationsCountLabel => Format(GetMissingTranslationCountPerLanguage());

        private string Format(Dictionary<string, int> missingTranslationCountPerLanguage)
        {
            return string.Join(" ", missingTranslationCountPerLanguage
                .Where(p => p.Value > 0)
                .OrderBy(p => p.Key)
                .Select(p => $"{p.Key}:{p.Value}"));
        }

        /// <summary>
        /// Recursively finds all missing translation coutns per language.
        /// </summary>
        private Dictionary<string, int> GetMissingTranslationCountPerLanguage()
        {
            var d = IsTextItem 
                ? Translations.GroupBy(t => t.Language)
                .ToDictionary(t => t.Key, t => t.Count(tt => string.IsNullOrWhiteSpace(tt.Text)))
                : new();

            foreach (var child in Children)
            {
                foreach (var pair in child.GetMissingTranslationCountPerLanguage())
                {
                    if (d.ContainsKey(pair.Key))
                    {
                        d[pair.Key] += pair.Value;
                    }
                    else
                    {
                        d.Add(pair.Key, pair.Value);
                    }
                }

            }

            return d;
        }

        public bool IsDefaultKey { get; set; }
        public bool IsNew => IsTextItem && Key == "NEW";

        public int TextCount => IsTextItem ? 1 : 0 + Children.Sum(c => c.TextCount);

        /// <summary>
        /// All places in source code where this token is used.
        /// </summary>
        public List<TokenUsage> TokenUsages
        {
            get => _tokenUsages;
            set
            {
                if (value == _tokenUsages) return;
                _tokenUsages = value;
                OnPropertyChanged();
                SelectedTokenUsage = TokenUsages.FirstOrDefault();
                TokenUsageSelectionIndex = 1;
            }
        }

        public TokenUsage SelectedTokenUsage
        {
            get => _selectedTokenUsage;
            set
            {
                if (value == _selectedTokenUsage) return;
                _selectedTokenUsage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 1-based, human readable.
        /// </summary>
        public int TokenUsageSelectionIndex
        {
            get => _tokenUsageSelectionIndex;
            set
            {
                if (value == _tokenUsageSelectionIndex) return;
                _tokenUsageSelectionIndex = value;
                OnPropertyChanged();
            }
        }

        public TreeItem WalkToNextTreeItem(bool stepIntoChildren = true)
        {
            // walk to first child
            if (stepIntoChildren && Children.Any())
                return Children.First();

            if (Parent == null) // root
                return null;

            // walk to next sibling
            var idxAtParent = Parent.Children.IndexOf(this);
            if (Parent.Children.Count > idxAtParent + 1)
                return Parent.Children[idxAtParent + 1];

            // continue higher up the tree.
            return Parent.WalkToNextTreeItem(false);
        }

        public bool MatchesFilter(string searchText)
        {
            return Key.ToLower().Contains(searchText) || Translations != null && Translations.Any(t => t.Text != null && t.Text.ToLower().Contains(searchText));
        }

        /// <summary>
        /// Expands all parent TreeItems of this TreeItem to visually reveal it.
        /// </summary>
        public void ExpandParents()
        {
            var current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
                current.IsExpanded = true;
            }
        }
    }
}