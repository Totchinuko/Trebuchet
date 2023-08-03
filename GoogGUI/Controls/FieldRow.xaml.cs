using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoogGUI.Controls
{
    /// <summary>
    /// Interaction logic for FieldRow.xaml
    /// </summary>
    public partial class FieldRow : UserControl, INotifyPropertyChanged
    {
        private IGuiField _field;

        public FieldRow(IGuiField field)
        {
            InitializeComponent();
            _field = field;
            Field.ValueChanged += OnValueChanged;
            DataContext = this;
        }

        public event EventHandler<object?>? FieldChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public IGuiField Field { get => _field; private set => _field = value; }

        protected virtual void OnFieldChanged(object? value)
        {
            FieldChanged?.Invoke(this, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Field"));
        }

        private void OnValueChanged(object? sender, object? e)
        {
            OnFieldChanged(e);
        } 
    }
}
