using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogGUI
{
    public interface IPanel : ITemplateHolder, ICommand, INotifyPropertyChanged
    {
        bool Active { get; set; }

        ImageSource Icon { get; }

        string Label { get; }
    }
}