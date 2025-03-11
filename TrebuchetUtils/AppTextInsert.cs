using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetUtils
{
    public class AppTextInsert : INotifyPropertyChanged
    {
        private string[] _args;
        private string _key;

        public AppTextInsert(string key, params string[] args)
        {
            _key = key;
            _args = args;
            AppText.Instance.TextSetChanged += OnTextChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Value
        {
            get
            {
                return AppText.Get(_key, _args);
            }
        }

        public static implicit operator string(AppTextInsert insert)
        {
            return insert.Value;
        }

        public void SetArgs(params string[] args)
        {
            _args = args;
            OnPropertyChanged(nameof(Value));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnTextChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Value));
        }
    }
}