using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NgLocalizer.Configuration;
using NgLocalizer.ViewModels;

namespace NgLocalizer.Parsing
{
    internal class TextItemExtractor
    {
        private readonly IFileParser[] _fileParsers = { new HtmlParser(), new TypeScriptParser() };

        private readonly string _sourceFolder;
        private readonly LanguagesConfiguration _languagesConfig;
        private int _folderCount;
        private int _foldersDone;
        private readonly TranslationPersister _translationPersister;
        private Dictionary<string, JObject> _translations;

        public TextItemExtractor(string sourceFolder, LanguagesConfiguration languagesConfig, TranslationPersister translationPersister)
        {
            _sourceFolder = sourceFolder;
            _languagesConfig = languagesConfig;
            _translationPersister = translationPersister;
        }

        public async Task<TreeItem> ExtractAsync()
        {
            return await Task.Factory.StartNew(() =>
                {
                    Progress?.Invoke(this, 0f);
                    _translations = LoadTranslationFiles();
                    _folderCount = CountFolders(_sourceFolder);
                    _foldersDone = 0;
                    try
                    {
                        var rootItems = ProcessFolder(_sourceFolder);
                        PostProcessItems(rootItems);
                        return AddSingleRootParent(rootItems);
                    }
                    finally
                    {
                        Progress?.Invoke(this, 1f);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
                );
        }

        private static TreeItem AddSingleRootParent(List<TreeItem> items)
        {
            var root = new TreeItem
            {
                Key = "root", 
                Children = items 
            };
            foreach (var item in items)
            {
                item.Parent = root;
            }

            return root;
        }

        private static void PostProcessItems(List<TreeItem> items)
        {
            foreach (var treeItem in items)
            {
                if (treeItem.IsTextItem && treeItem.IsValid)
                {
                    treeItem.IsDefaultKey = treeItem.Key.StartsWith(KeyNameGenerator.GetNamespace(treeItem));
                }
                else
                {
                    treeItem.IsDefaultKey = true;
                    PostProcessItems(treeItem.Children);
                }
            }
        }

        private Dictionary<string, JObject> LoadTranslationFiles()
        {
            var lookups = new Dictionary<string, JObject>();
            foreach (var language in _languagesConfig.AllLanguages)
            {
                var obj = _translationPersister.LoadTranslationSet(language);
                lookups.Add(language, obj);
            }

            return lookups;
        }

        private static int CountFolders(string sourceFolder)
        {
            return 1 + Directory.GetDirectories(sourceFolder).Sum(CountFolders);
        }

        private List<TreeItem> ProcessFolder(string folder, TreeItem parent = null)
        {
            var treeItems = ParseAllFilesInFolder(folder, parent);

            foreach (var directory in Directory.GetDirectories(folder))
                ProcessSubFolder(directory, treeItems, parent);

            _foldersDone++;
            Progress?.Invoke(this, (float)_foldersDone / _folderCount);

            return treeItems.Any()
                ? treeItems.OrderBy(i => !i.IsFolder).ThenBy(i => i.Key).ToList()
                : treeItems;
        }

        private List<TreeItem> ParseAllFilesInFolder(string folder, TreeItem parent)
        {
            var list = new List<TreeItem>();

            foreach (var parser in _fileParsers)
            {
                foreach (var file in Directory.GetFiles(folder, parser.FileMask))
                {
                    list.AddRange(GetTreeItemsFromFile(file, parent, parser));
                }
            }

            return list;
        }

        private IEnumerable<TreeItem> GetTreeItemsFromFile(string file, TreeItem parent, IFileParser parser)
        {
            var tokenUsages = parser.ParseFile(file);

            var fileTreeItem = new TreeItem
            {
                FullFilename = file,
                Key = Path.GetFileName(file),
                Parent = parent,
                LastSourceFileWrite = File.GetLastWriteTime(file)
            };

            var textTreeItems = ConvertToTextTreeItems(fileTreeItem, tokenUsages);

            if (textTreeItems.Any())
            {
                fileTreeItem.Children = textTreeItems;
                yield return fileTreeItem; // only produce TreeItem if it has children
            }
        }

        private List<TreeItem> ConvertToTextTreeItems(TreeItem parent, IEnumerable<TokenUsage> tokenUsages)
        {
            return tokenUsages.GroupBy(usage => usage.Token).Select(t =>
                new TreeItem
                {
                    Key = t.Key,
                    TokenUsages = t.ToList(),
                    Translations = BuildTranslationsFor(t.Key),
                    FullFilename = parent.FullFilename,
                    Parent = parent
                }).ToList();
        }

        private List<Translation> BuildTranslationsFor(string key)
        {
            var translations = new List<Translation>();
            var keyPath = key.Split('.');
            foreach (var language in _translations.Keys)
            {
                var dictionary = _translations[language];
                var current = (JToken)dictionary;
                foreach (var pathPart in keyPath)
                {
                    if (current is JObject obj && obj.ContainsKey(pathPart))
                    {
                        current = current[pathPart];
                    }
                    else
                    {
                        // no translations exist yet, or path passes as JValue
                        current = null;
                        break;
                    }
                }

                translations.Add(new Translation
                {
                    Language = language,
                    Text = current is JValue jValue ? jValue.Value<string>() : null
                });
            }

            return translations;
        }

        private void ProcessSubFolder(string directory, List<TreeItem> list, TreeItem parent)
        {
            var key = Path.GetFileName(directory);
            var directoryItem = new TreeItem
            {
                Key = key,
                IsFolder = true,
                Parent = parent
            };
            var children = ProcessFolder(directory, directoryItem);
            if (children.Any())
            {
                // only add the parent node if it has children
                list.Add(directoryItem);
                directoryItem.Children = children;
            }
        }

        public event EventHandler<float> Progress;
    }
}
