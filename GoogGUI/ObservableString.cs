using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GoogGUI
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

        public static List<string> ToList(TrulyObservableCollection<ObservableString> value)
        {
            return value.ToList().ConvertAll((x) => x.value);
        }

        public static TrulyObservableCollection<ObservableString> ToObservableList(List<string> value)
        {
            return new TrulyObservableCollection<ObservableString>(value.ConvertAll((x) => new ObservableString { value = x }));
        }
    }
}