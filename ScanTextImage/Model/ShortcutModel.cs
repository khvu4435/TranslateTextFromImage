using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ScanTextImage.Model
{
    public class ShortcutModel : INotifyPropertyChanged
    {
        private bool _IsControlKey;
        private bool _IsShiftKey;
        private bool _IsAltKey;
        private string _Key;

        private KeyGesture _gesture;
        private event Action _action;

        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public bool IsControlKey
        {
            get
            {
                return _IsControlKey;
            }
            set
            {
                this._IsControlKey = value;
                this.NotifyPropertyChanged(nameof(ShortcutModel.IsControlKey));
            }
        }

        public bool IsShiftKey
        {
            get
            {
                return _IsShiftKey;
            }
            set
            {
                this._IsShiftKey = value;
                this.NotifyPropertyChanged(nameof(ShortcutModel.IsShiftKey));
            }
        }

        public bool IsAltKey
        {
            get
            {
                return _IsAltKey;
            }
            set
            {
                this._IsAltKey = value;
                this.NotifyPropertyChanged(nameof(ShortcutModel.IsAltKey));
            }
        }

        public string Key
        {
            get
            {
                return _Key;
            }
            set
            {
                this._Key = value;
                this.NotifyPropertyChanged(nameof(ShortcutModel.Key));
            }
        }

        [JsonIgnore]
        public KeyGesture Gesture
        {
            get => _gesture;
            set
            {
                _gesture = value;
                NotifyPropertyChanged(nameof(ShortcutModel.Gesture));
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
