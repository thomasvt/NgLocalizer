using System.Windows;
using System.Windows.Controls;
using NgLocalizer.ViewModels;

namespace NgLocalizer.CodePreviewing
{
    /// <summary>
    /// Interaction logic for CodePreview.xaml
    /// </summary>
    public partial class CodePreview : UserControl
    {
        public CodePreview()
        {
            InitializeComponent();
        }

        private void OpenSourceFile_Click(object sender, RoutedEventArgs e)
        {
            var treeItem = DataContext as TreeItem;
            EditAppIntegration.RevealSelectedTokenUsageInVSCode(treeItem);
        }

        private void PreviousButtonClick(object sender, RoutedEventArgs e)
        {
            var treeItem = DataContext as TreeItem;
            treeItem.SelectPreviousTokenUsage();
        }

        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            var treeItem = DataContext as TreeItem;
            treeItem.SelectNextTokenUsage();
        }
    }
}
