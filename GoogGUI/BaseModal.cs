using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public abstract class BaseModal
    {
        public abstract int Height { get; }
        public abstract string ModalTitle { get; }
        public abstract DataTemplate Template { get; }
        public abstract int Width { get; }

        public abstract void OnWindowClose();
    }
}