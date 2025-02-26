using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NgLocalizer.ViewModels;

namespace NgLocalizer
{
    public class TreeViewItemRevealer
    {
        private readonly TreeView _treeView;

        public TreeViewItemRevealer(TreeView treeView)
        {
            _treeView = treeView;
        }

        // some batshit crazy code below!

        // explanation:
        // We want to scroll a treeitem (viewmodel) into view. To do that we must expand the parent nodes of the treeitem. But as they aren't generated yet, we need to wait for that from their ContainerGenerator.
        // But eacht sub-branch has its own ContainerGenerator, so we need to recursively wait for branches to be created until we reach the item we are looking for. Only then, we can let the treeview scroll to that item.


        /// <summary>
        /// Brings a viewmodel bound in a treeview into view by expanding the necessary parents and scrolling the tree.
        /// </summary>
        /// <param name="treeItem"></param>
        public void BringIntoView(TreeItem treeItem)
        {
            treeItem.ExpandParents();
            treeItem.IsSelected = true;
            Task.Run(() =>
            {
                var itemContainer = GetItemContainerFromTreeItem(treeItem);
                if (itemContainer != null)
                {
                    _treeView.Dispatcher.Invoke(() =>
                    {
                        _treeView.Focus();
                        itemContainer.BringIntoView();
                    });
                }
            });
        }

        private TreeViewItem GetItemContainerFromTreeItem(TreeItem treeItem)
        {
            var container = _treeView.ItemContainerGenerator.ContainerFromItem(treeItem);
            if (container == null)
            {
                var containerGenerator = GetItemContainerFromTreeItem(treeItem.Parent).ItemContainerGenerator;
                SpinWait.SpinUntil(() => containerGenerator.Status == GeneratorStatus.ContainersGenerated);
                container = containerGenerator.ContainerFromItem(treeItem);
            }

            return (TreeViewItem)container;
        }
    }
}
