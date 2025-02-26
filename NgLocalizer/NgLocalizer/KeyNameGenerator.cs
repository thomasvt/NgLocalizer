using System;
using System.Collections.Generic;
using System.Linq;
using NgLocalizer.ViewModels;

namespace NgLocalizer
{
    public static class KeyNameGenerator
    {
        public static string GetNamespace(TreeItem treeItem)
        {
            var parents = new List<string>();

            var currentItem = treeItem;
            while (currentItem.Parent != null)
            {
                currentItem = currentItem.Parent;
                if (currentItem.IsFolder && currentItem.Key != "ROOT") // exclude file-nodes and the tree root
                    parents.Add(currentItem.Label);
            }

            parents.Reverse();

            return $"{string.Join(".", parents)}.".ToUpper();
        }

        public static string GetDefaultKeyNameFor(TreeItem treeItem)
        {
            var ns = GetNamespace(treeItem);
            if (treeItem.Key.StartsWith(ns))
                return treeItem.Key; // current key already follows the namespace convension

            // current key does not follow the convension, propose a changed key that does follow the convension:
            var lastKeyPart = treeItem.Key.Split('.').Last();
            if (string.IsNullOrWhiteSpace(lastKeyPart))
                throw new Exception($"Last part of existing key must be non-empty: {treeItem.Key}");

            return $"{ns}{lastKeyPart.ToUpper()}";
        }
    }
}