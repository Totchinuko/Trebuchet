using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Trebuchet
{
    public class ObservableString : INotifyPropertyChanged
    {
        private string value = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Value
        {
            get => value;
            set
            {
                this.value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
            }
        }

        public static implicit operator ObservableString(string value) => new ObservableString { value = value };

        public static implicit operator string(ObservableString value) => value.value;
    }
}