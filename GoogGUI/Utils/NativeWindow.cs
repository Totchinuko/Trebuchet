using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace GoogGUI
{
    class NativeWindow : IWin32Window
    {
        private readonly IntPtr _handle;
        public NativeWindow(IntPtr handle)
        {
            _handle = handle;
        }

        IntPtr IWin32Window.Handle
        {
            get { return _handle; }
        }

        public static IWin32Window GetIWin32Window(Visual visual)
        {
            HwndSource? source = PresentationSource.FromVisual(visual) as HwndSource;
            if (source == null)
                throw new NullReferenceException($"source is not found, no reference set");
            IWin32Window win = new NativeWindow(source.Handle);
            return win;
        }

    }
}
