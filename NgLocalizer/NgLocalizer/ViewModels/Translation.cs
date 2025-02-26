namespace NgLocalizer.ViewModels
{
    public class Translation
    : ViewModel
    {
        private string _text;
        public string Language { get; set; }

        public string Text
        {
            get => _text;
            set
            {
                if (value == _text) return;
                _text = value;
                OnPropertyChanged();
            }
        }
    }
}
