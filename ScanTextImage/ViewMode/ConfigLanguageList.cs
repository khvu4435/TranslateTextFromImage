using ScanTextImage.Model;
using System.ComponentModel;

namespace ScanTextImage.ViewMode
{
    public class ConfigLanguageList : INotifyPropertyChanged
    {
        private bool _isSelected;
        private LanguageModel _languageModel;

        public bool isSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; NotifyPropertyChanged(nameof(isSelected)); }
        }

        public LanguageModel LanguageModel
        {
            get { return _languageModel; }
            set { _languageModel = value; NotifyPropertyChanged(nameof(LanguageModel)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
