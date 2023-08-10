using Goog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public abstract class FieldEditorPanel : Panel
    {
        private List<IField> _fields = new List<IField>();
        private ObservableCollection<RequiredCommand> _requiredActions = new ObservableCollection<RequiredCommand>();

        protected FieldEditorPanel(Config config) : base(config)
        {
        }

        protected virtual void BuildFields()
        {
            _fields = IField.BuildFieldList(this);
            OnPropertyChanged("Fields");
        }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["FieldEditor"];

        public List<IField> Fields { get => _fields; set => _fields = value; }

        public ObservableCollection<RequiredCommand> RequiredActions { get => _requiredActions; set => _requiredActions = value; }
    }
}
