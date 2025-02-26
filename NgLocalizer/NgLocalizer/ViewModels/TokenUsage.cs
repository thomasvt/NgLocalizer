namespace NgLocalizer.ViewModels
{
    public class TokenUsage : ViewModel
    {

        private string _codeViewBeforeKey;
        private string _codeViewAfterKey;
        private string _codeViewKey;

        public string Token { get; set; }
        public int Begin { get; set; }
        public int Length { get; set; }
        public string FullFilename { get; set; }

        public string CodeViewBeforeKey
        {
            get => _codeViewBeforeKey;
            set
            {
                if (value == _codeViewBeforeKey) return;
                _codeViewBeforeKey = value;
                OnPropertyChanged();
            }
        }

        public string CodeViewAfterKey
        {
            get => _codeViewAfterKey;
            set
            {
                if (value == _codeViewAfterKey) return;
                _codeViewAfterKey = value;
                OnPropertyChanged();
            }
        }

        public string CodeViewKey
        {
            get => _codeViewKey;
            set
            {
                if (value == _codeViewKey) return;
                _codeViewKey = value;
                OnPropertyChanged();
            }
        }
    }
}
