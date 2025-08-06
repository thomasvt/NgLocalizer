using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using NgLocalizer.Configuration;
using NgLocalizer.Parsing;
using NgLocalizer.ViewModels;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using TreeView = System.Windows.Controls.TreeView;

namespace NgLocalizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<TreeItem> _rootItems;
        private TextItemExtractor _extractor;
        private TranslationPersister _translationPersister;
        private CodeBaseWatcher _sourceWatcher;
        private CodeBaseWatcher _i18nFolderWatcher;
        private LanguagesConfiguration _languagesConfiguration;
        private SettingsService _settings;
        private TokenRenamer _tokenRenamer;
        private bool _sourcesDirty;
        private string _sourceFolder;
        private string _i18NFolder;
        private string _searchText;
        private Language _selectedLanguage;
        private List<Language> _languages;
        private Translation _selectedTranslation;
        private string _selectedTranslationUpdatedPerTextChange;

        private void CreateServices()
        {
            _settings = new SettingsService();
            _settings.Load();
            SourceFolder = _settings.SourceFolder;
            I18NFolder = _settings.I18NFolder;

            if (!Directory.Exists(_settings.SourceFolder) || !Directory.Exists(_settings.I18NFolder))
            {
                MessageBox.Show("Configured Source or i18n folder not found, reconfigure them using config menu.", "Bad config", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _languagesConfiguration = new LanguagesConfiguration();
            _languagesConfiguration.Load();
            Languages = _languagesConfiguration.AllLanguages.Select(l => new Language() { Name = l }).ToList();
            
            if (!Languages.Any())
                throw new Exception("Must have at least one language in appconfig.");

            SelectedLanguage = Languages.First();
            SelectedLanguage.IsSelected = true;

            _sourceWatcher = new CodeBaseWatcher(_settings.SourceFolder);
            _sourceWatcher.ChangeDetected += MarkSourcesDirty;
            _sourceWatcher.Initialize();

            _i18nFolderWatcher = new CodeBaseWatcher(_settings.I18NFolder);
            _i18nFolderWatcher.ChangeDetected += MarkSourcesDirty;
            _i18nFolderWatcher.Initialize();

            _translationPersister = new TranslationPersister(_settings.I18NFolder);
            _extractor = new TextItemExtractor(_settings.SourceFolder, _languagesConfiguration, _translationPersister);
            _tokenRenamer = new TokenRenamer(_languagesConfiguration, _translationPersister);
        }

        public List<Language> Languages
        {
            get => _languages;
            set
            {
                if (Equals(value, _languages)) return;
                _languages = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            CreateServices();
        }

        public List<TreeItem> RootItems
        {
            get => _rootItems;
            set
            {
                if (Equals(value, _rootItems)) return;
                _rootItems = value;
                OnPropertyChanged();
            }
        }

        private async void ScanSources_Click(object sender, RoutedEventArgs e)
        {
            await ScanSourcesAsync();
        }

        private async Task ScanSourcesAsync()
        {
            if (_extractor == null)
                return;

            SourceScanner.Visibility = Visibility.Visible;
            IsEnabled = false;
            // We often receive events while scanning on files that didn't actually change.
            // I think it has to do with NTFS cache + .NET6 because it doesn't happen on consecutive scans,
            // and we never had this for years in pre .NET6.
            // Therefore, we disable the watchers during scan:
            _sourceWatcher.Enable(false); 
            _i18nFolderWatcher.Enable(false);
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 0;
            await ChangeDetailPanelItem(null);

            _extractor.Progress += (o, f) => { ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = (int)(f * 100)); };
            try
            {
                var oldItems = RootItems; // current tree version
                var newItems = (await _extractor.ExtractAsync()).Children; // rescan current files for new version of tree, don't use the root parent in UI (has logical needs only)
                ApplyUIState(newItems, oldItems); // copy UI state from old tree to new version (expanded nodes etc)
                OpenAllNewKeys(newItems); // expand to show "NEW" keys, so the user can easily find them
                RootItems = newItems; // replace new version
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error reading sources.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _sourcesDirty = false;
                SourceScanner.Visibility = Visibility.Collapsed;
                IsEnabled = true;
                _sourceWatcher.Enable(true);
                _i18nFolderWatcher.Enable(true);
            }
        }

        private static void OpenAllNewKeys(List<TreeItem> newItems)
        {
            foreach (var item in newItems)
            {
                if (item.IsTextItem && item.Key == "NEW")
                    item.ExpandParents();
                else
                    OpenAllNewKeys(item.Children);
            }
        }

        private async void MarkSourcesDirty()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                if (!IsActive) // while in other app, remember that a filechange is detected. We will effectively rescan sources when window is activated again.
                {
                    _sourcesDirty = true;
                }
                else
                {
                    await ScanSourcesAsync();
                }
            });
        }

        /// <summary>
        /// Copies the UI state of the old to the new (corresponding) treenodes.
        /// </summary>
        private void ApplyUIState(List<TreeItem> newItems, List<TreeItem> oldItems)
        {
            if (newItems == null || oldItems == null)
                return;
            foreach (var newItem in newItems)
            {
                var oldItem = oldItems.FirstOrDefault(i => i.Key == newItem.Key);
                if (oldItem != null)
                {
                    newItem.IsExpanded = oldItem.IsExpanded;
                    newItem.IsSelected = oldItem.IsSelected;
                    if (newItem.IsSelected)
                        DetailPanel.Content = newItem;
                    ApplyUIState(newItem.Children, oldItem.Children);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            await ChangeDetailPanelItem(((TreeView)sender).SelectedValue as TreeItem);
        }

        private async Task ChangeDetailPanelItem(TreeItem treeItem)
        {
            if (DetailPanel.Content is TreeItem existingTreeItem)
            {
                RemoveChangeHandlers(existingTreeItem);
            }

            if (treeItem is { IsTextItem: true })
            {
                DetailPanel.Visibility = Visibility.Visible;
                DetailPanel.Content = treeItem;
                UpdateSelectedTranslation();
                HookChangeHandlers(treeItem);
                await SourcePreviewLoader.LoadSourceCodePreviewAsync(treeItem);
            }
            else
            {
                DetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void HookChangeHandlers(TreeItem treeItem)
        {
            foreach (var translation in treeItem.Translations)
            {
                translation.PropertyChanged += TranslationOnPropertyChanged;
            }
        }

        private void RemoveChangeHandlers(TreeItem existingTreeItem)
        {
            if (existingTreeItem.Translations == null)
                return;
            foreach (var translation in existingTreeItem.Translations)
            {
                translation.PropertyChanged -= TranslationOnPropertyChanged;
            }
        }

        private void TranslationOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                var translation = sender as Translation;
                var treeItem = TreeView.SelectedValue as TreeItem;

                EnableFileWatchers(false);
                try
                {
                    _translationPersister.SaveTranslationText(translation.Language, treeItem.Key, translation.Text);
                }
                finally
                {
                    EnableFileWatchers(true);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error saving", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableFileWatchers(bool isEnabled)
        {
            _sourceWatcher.Enable(isEnabled);
            _i18nFolderWatcher.Enable(isEnabled);
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            _sourceWatcher?.Dispose();
            _i18nFolderWatcher?.Dispose();
        }

        private async void MainWindow_OnActivated(object sender, EventArgs e)
        {
            if (_sourcesDirty)
                await ScanSourcesAsync();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            Title = $"NgLocalizer v{version.Major}.{version.Minor}";
            await ScanSourcesAsync();
        }

        private async void RenameKey_Click(object sender, RoutedEventArgs e)
        {
            var treeItem = TreeView.SelectedValue as TreeItem;
            if (!treeItem.IsTextItem)
            {
                MessageBox.Show("Renaming keys is only supported on single text items (leaf nodes).");
                return;
            }
            if (treeItem.Key != "NEW" && CountKeysInCodeBase(treeItem.Key, treeItem) > 0)
            {
                MessageBox.Show($"Renaming keys that are used in multiple places is not supported. (key \"{treeItem.Key}\" is used in {CountKeysInCodeBase(treeItem.Key, treeItem)} other places).", "Cannot rename key", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newKey = treeItem.Key; // KeyNameGenerator.GetDefaultKeyNameFor(treeItem);
            var dialog = new RenamePopupWindow
            {
                CurrentKey = treeItem.Key,
                NewKey = newKey
            };
            if (dialog.ShowDialog() == true)
            {
                newKey = dialog.NewKey;
                if (newKey == "NEW")
                {
                    MessageBox.Show($"Renaming a key to \"NEW\" is not allowed.", "Cannot rename key", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var usageCount = CountKeysInCodeBase(newKey, null);
                if (usageCount > 0)
                {
                    MessageBox.Show($"Key \"{newKey}\" is already used in {usageCount} other place(s).", "Cannot rename key", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    EnableFileWatchers(false);
                    _tokenRenamer.Rename(treeItem, newKey);
                    await SourcePreviewLoader.LoadSourceCodePreviewAsync(treeItem); // update code preview
                    await ScanSourcesAsync(); // update source text indexes, we changed a file
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Key name fix failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    EnableFileWatchers(true);
                }
            }
        }

        /// <summary>
        /// Counts the number of keys in the entire codebase.
        /// </summary>
        private int CountKeysInCodeBase(string key, TreeItem excludeItem, List<TreeItem> treeItems = null)
        {
            if (treeItems == null)
                treeItems = RootItems;

            var count = 0;
            foreach (var treeItem in treeItems)
            {
                if (treeItem.IsTextItem)
                {
                    if (treeItem.Key == key && treeItem != excludeItem)
                        count++;
                }
                else
                {
                    count += CountKeysInCodeBase(key, excludeItem, treeItem.Children);
                }
            }

            return count;
        }

        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item)
            {
                item.Focus(); // select the item
                e.Handled = true;
            }
        }

        private void FrameworkElement_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (TreeView.SelectedValue is TreeItem treeItem)
            {
                if (treeItem.IsTextItem && treeItem.IsValid)
                {
                    // it's a localizable key
                    RenameKeyMenuItem.Visibility = Visibility.Visible;
                    CopyKeyMenuItem.Visibility = Visibility.Visible;
                    return;
                }

                if (treeItem.HasSourceFile || treeItem.IsTextItem)
                {
                    // not a localization key, but linked to a source file.
                    RenameKeyMenuItem.Visibility = Visibility.Collapsed;
                    CopyKeyMenuItem.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            e.Handled = true;
        }

        private void CopyNewKeyHtmlCode(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("{{ 'NEW' | translate }}");
        }

        private void CopyNewKeyTsCode(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("this.translateService.instant('NEW')");
        }

        private async void SetAppFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog
            {
                SelectedPath = _settings.SourceFolder,
                Description = "Pick src\\app folder",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _settings.SourceFolder = dlg.SelectedPath;
                SourceFolder = _settings.SourceFolder;
                await SaveConfigAsync();
            }
        }

        private async Task SaveConfigAsync()
        {
            await _settings.SaveAsync();
            if (Directory.Exists(_settings.SourceFolder) && Directory.Exists(_settings.I18NFolder))
            {
                CreateServices();
                await ScanSourcesAsync();
            }
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog
            {
                SelectedPath = _settings.I18NFolder,
                Description = "Pick i18n folder",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _settings.I18NFolder = dlg.SelectedPath;
                I18NFolder = _settings.I18NFolder;
                await SaveConfigAsync();
            }
        }

        private void CopyKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var treeItem = TreeView.SelectedValue as TreeItem;
            if (treeItem.IsTextItem)
            {
                Clipboard.SetText(treeItem.Key);
            }
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            MessageBox.Show($"NgLocalizer v{version.Major}.{version.Minor}.\n\nThis is a tool developed through the years for managing ngxTranslate translations a lot easier than manually. Please stay cautious and check the changes this tool makes before committing.", "About", MessageBoxButton.OK);
        }

        private void HelpMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(@"SETUP
* Configure the 2 paths to your source code and translation file folder using the Config menu.
* You can add more languages by changing the app.config 'languages' key. An i18n file for the new language will be created by ngLocalizer.

ADD NEW TRANSLATABLE ITEMS
* When you use 'NEW' as name for translatable tokens in your sources, the tool will automatically detect and reveal them in the tree for your convenience.
* The Code Snippets menu allows to put HTML or Typescript syntax for such a 'NEW' translation item on your clipboard.

TRANSLATING ITEMS
* Scan your sources with the button in the bottom to show a tree of the localizable tokens in your sources. Autoscanning happens upon each detected file-change.
* The tree also shows how many translatable items there are below each node (in black), and how many not-yet-translated items (in red).
* Rename tokens (right click menu): when renaming tokens, ngLocalizer suggests a namespaced key based on the file-path of the token. If you follow this convention, the tokens in the tree will stop showing the full namespace.
* ngLocalizer tolerates the reuse of tokens (eg. 'GLOBAL.OK' for all ok buttons) but it is not built around this being a good practice in the long run. Therefore some features will falter a bit. eg. renaming a token will ONLY rename the usages of that token in the one file where you initiated the rename (in the treeview), not in other files where the same token may have been used.
  

TREE COLOR LEGEND
* bold text = valid token but deviating from ngLocalizer's naming convention
* gray text = invalid token", "Help/tips", MessageBoxButton.OK);
        }

        private void OpenSourceFileInDefaultEditor_Click(object sender, RoutedEventArgs e)
        {
            var treeItem = TreeView.SelectedValue as TreeItem;
            EditAppIntegration.OpenSourceFileInDefaultEditor(treeItem);
        }
        
        private void OpenSourceFile_Click(object sender, RoutedEventArgs e)
        {
            var treeItem = TreeView.SelectedItem as TreeItem;
            EditAppIntegration.RevealInVSCode(treeItem);
        }

        public string SourceFolder
        {
            get => _sourceFolder;
            set
            {
                if (value == _sourceFolder) return;
                _sourceFolder = value;
                OnPropertyChanged();
            }
        }

        public string I18NFolder
        {
            get => _i18NFolder;
            set
            {
                if (value == _i18NFolder) return;
                _i18NFolder = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (value == _searchText) return;
                _searchText = value;
                OnPropertyChanged();
            }
        }

        private void SearchNext(object sender, RoutedEventArgs e)
        {
            if (!RootItems.Any() || string.IsNullOrWhiteSpace(SearchText))
                return;

            var startItem = TreeView.SelectedItem as TreeItem;

            var treeItem = new TreeSearcher(RootItems).FindNext(startItem, SearchText.Trim());
            if (treeItem != null)
            {
                new TreeViewItemRevealer(TreeView).BringIntoView(treeItem);
            }
            else
            {
                MessageBox.Show("Nothing found.", "No hit", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        private void LanguageRadioButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedLanguage = (e.Source as FrameworkElement)?.DataContext as Language;
            UpdateSelectedTranslation();
        }

        private void UpdateSelectedTranslation()
        {
            SelectedTranslation = (DetailPanel.Content as TreeItem)?.Translations.SingleOrDefault(t => t.Language == SelectedLanguage?.Name);
        }

        public Language? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (Equals(value, _selectedLanguage)) return;
                _selectedLanguage = value;
                OnPropertyChanged();
            }
        }

        public Translation? SelectedTranslation
        {
            get => _selectedTranslation;
            set
            {
                if (Equals(value, _selectedTranslation)) return;
                _selectedTranslation = value;
                SelectedTranslationUpdatedPerTextChange = _selectedTranslation?.Text;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The translated text binding does not use UpdateSourceTrigger PropertyChanged because then we would save the translation file each keystroke.
        /// To update the markdown preview each stroke, though, we have this extra property that is updated by change events on the Textbox too.
        /// </summary>
        public string SelectedTranslationUpdatedPerTextChange
        {
            get => _selectedTranslationUpdatedPerTextChange;
            set
            {
                if (value == _selectedTranslationUpdatedPerTextChange) return;
                _selectedTranslationUpdatedPerTextChange = value;
                OnPropertyChanged();
            }
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            var t = sender as TextBox;
            SelectedTranslationUpdatedPerTextChange = t.Text;
        }
    }
}
