using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Model
{
    public class DownloadItem : INotifyPropertyChanged
    {
        private double _progressPercent;
        private string _progressStatus;
        private LanguageModel _langModel;

        public double progressPercent
        {
            get => _progressPercent;
            set
            {
                _progressPercent = value;
                this.NotifyPropertyChanged(nameof(progressPercent));
            }
        }
        public string progressStatus
        {
            get => _progressStatus;
            set
            {
                _progressStatus = value;
                this.NotifyPropertyChanged(nameof(progressStatus));
            }
        }

        public LanguageModel langModel
        {
            get => _langModel;
            set
            {
                _langModel = value;
                this.NotifyPropertyChanged(nameof(langModel));
            }
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
