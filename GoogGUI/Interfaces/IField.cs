using System.Windows.Input;

namespace GoogGUI
{
    public interface IField
    {
        public string FieldName { get; set; }

        public bool IsDefault { get; }

        public string Property { get; }

        public ICommand ResetCommand { get; }

        public object Template { get; }

        public object? Value { get; set; }
    }
}