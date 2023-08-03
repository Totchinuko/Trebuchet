using Goog;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for SettingsContent.xaml
    /// </summary>
    public partial class SettingsContent : UserControl, IGUIPanel
    {
        private List<FieldRow> _rows = new List<FieldRow>(); 

        public SettingsContent()
        {
            InitializeComponent();
            DataContext = this;
        }

        public List<FieldRow> Rows { get => _rows; set => _rows = value; }

        public void Close()
        {
            _rows.Clear();
        }

        public void Setup(Config config, Profile? profile)
        {
            Type type = typeof(Config);
            _rows = new List<FieldRow>
            {
                new FieldRow(new FieldDirFinder("Install Path").SetField(config, "InstallPath", string.Empty)),
                new FieldRow(new FieldDirFinder("Client Path").SetField(config, "ClientPath", string.Empty)),
            };
        }
    }
}
