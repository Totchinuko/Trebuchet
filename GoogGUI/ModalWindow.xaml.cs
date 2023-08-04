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
using System.Windows.Shapes;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for ModalWindow.xaml
    /// </summary>
    public partial class ModalWindow : Window
    {
        private BaseModal _app;

        public ModalWindow(BaseModal modal)
        {
            InitializeComponent();
            _app = modal;
            DataContext = this;
        }

        protected override void OnClosed(EventArgs e)
        {
            _app.OnWindowClose();
            base.OnClosed(e);
        }

        public BaseModal App { get => _app; private set => _app = value; }
    }
}