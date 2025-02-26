using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using NgLocalizer.Configuration;
using NgLocalizer.ViewModels;

namespace NgLocalizer
{
    internal class TokenRenamer
    {
        private readonly LanguagesConfiguration _languagesConfiguration;
        private readonly TranslationPersister _translationPersister;

        public TokenRenamer(LanguagesConfiguration languagesConfiguration, TranslationPersister translationPersister)
        {
            _languagesConfiguration = languagesConfiguration;
            _translationPersister = translationPersister;
        }

        /// <summary>
        /// Renames the key of a token, and updates all places in source code and translation files.
        /// </summary>
        /// <param name="newKey">The new key of the token.</param>
        public void Rename(TreeItem treeItem, string newKey)
        {
            if (treeItem.FullFilename == null)
            {
                MessageBox.Show("That Key item has no Source File associated with it.", "Key-name fix", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RenameAllTokenUsages(treeItem, newKey);

            foreach (var language in _languagesConfiguration.AllLanguages)
            {
                _translationPersister.RemoveTranslationText(language, treeItem.Key);
                _translationPersister.SaveTranslationText(language, newKey, treeItem.Translations.Single(t => t.Language == language).Text);
            }

            treeItem.Key = newKey;
        }

        private static void RenameAllTokenUsages(TreeItem treeItem, string newKey)
        {
            var fileContent = File.ReadAllText(treeItem.FullFilename);

            ValidateRename(fileContent, treeItem);

            foreach (var tokenUsage in treeItem.TokenUsages.OrderByDescending(tu => tu.Begin)) // replace from back to front, else we get string-index corruption due to the replacements.
            {
                fileContent = fileContent.Remove(tokenUsage.Begin, tokenUsage.Length);
                fileContent = fileContent.Insert(tokenUsage.Begin, newKey);
            }
            File.WriteAllText(treeItem.FullFilename, fileContent);
        }

        private static void ValidateRename(string fileContent, TreeItem treeItem)
        {
            var prevEnd = -1;
            foreach (var tokenUsage in treeItem.TokenUsages.OrderBy(tu => tu.Begin))
            {
                if (tokenUsage.Begin <= prevEnd)
                    throw new Exception("This item is used more than once in this file, and the locations in the source file of those usages seem to overlap, which doesn't make sense. So, I cannot rename this key for you, you'll have to rename this key manually in code.");
                prevEnd = tokenUsage.Begin + tokenUsage.Length - 1;
            }

            foreach (var tokenUsage in treeItem.TokenUsages)
            {
                var textToReplace = fileContent.Substring(tokenUsage.Begin, tokenUsage.Length);
                if (textToReplace != treeItem.Key)
                    throw new CodeHasChangedException("The source file seems to have changed since the last scan. Rescan the sources and try again.");
            }
        }
    }
}
