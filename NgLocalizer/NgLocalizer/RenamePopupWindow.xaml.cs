using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NgLocalizer
{
    /// <summary>
    /// Interaction logic for RenamePopupWindow.xaml
    /// </summary>
    public partial class RenamePopupWindow : Window, INotifyPropertyChanged
    {
        private string _currentKey;
        private string _newKey;
        private string _suggestion;

        public RenamePopupWindow()
        {
            InitializeComponent();
        }

        private void RenamePopupWindow_OnActivated(object sender, EventArgs e)
        {
            KeyTextBox.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewKey) || NewKey == CurrentKey)
            {
                KeyTextBox.Focus();
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public string CurrentKey
        {
            get => _currentKey;
            set
            {
                if (value == _currentKey) return;
                _currentKey = value;
                OnPropertyChanged();
            }
        }

        public string NewKey
        {
            get => _newKey;
            set
            {
                if (value == _newKey) return;
                _newKey = value;
                OnPropertyChanged();
            }
        }

        public string Suggestion
        {
            get => _suggestion;
            set
            {
                if (value == _suggestion) return;
                _suggestion = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
