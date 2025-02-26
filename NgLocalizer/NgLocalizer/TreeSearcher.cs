using System.Collections.Generic;
using System.Linq;
using NgLocalizer.ViewModels;

namespace NgLocalizer
{
    /// <summary>
    /// (Not the best) tree walker. Looks for <see cref="TreeItem"/>s that match a searchFilter.
    /// </summary>
    public class TreeSearcher
    {
        private readonly List<TreeItem> _rootItems;

        public TreeSearcher(List<TreeItem> rootItems)
        {
            _rootItems = rootItems;
        }

        public TreeItem FindNext(TreeItem startExclusive, string searchText)
        {
            searchText = searchText.ToLower();

            var current = WalkToNextTreeItem(startExclusive);
            var last = current;

            if (current.MatchesFilter(searchText))
                return current;

            current = WalkToNextTreeItem(current);
            while (current != last)
            {
                if (current.MatchesFilter(searchText))
                    return current;

                current = WalkToNextTreeItem(current);
            }
            return null; // full cycle through tree but nothing matches.
        }

        private TreeItem WalkToNextTreeItem(TreeItem current)
        {
            return current?.WalkToNextTreeItem() ?? _rootItems.First(); // loop back to start of tree if end reached.
        }
    }
}
