using tot_gui_lib;

namespace Trebuchet.ViewModels
{
    public class ObservableString : BaseViewModel
    {
        private string _value = string.Empty;

        public string Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }
        public static implicit operator ObservableString(string value) => new() { _value = value };
        public static implicit operator string(ObservableString value) => value._value;
    }
}