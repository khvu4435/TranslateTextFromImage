using ScanTextImage.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.ViewMode
{
    public class ConfigLanguageList : INotifyPropertyChanged
    {
        private bool _isSelected;
        public LanguageModel LanguageModel { get; set; }

        public bool isSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; NotifyPropertyChanged(nameof(isSelected)); }
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
