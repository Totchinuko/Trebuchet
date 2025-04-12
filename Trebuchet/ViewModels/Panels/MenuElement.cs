using ReactiveUI;

namespace Trebuchet.ViewModels.Panels
{
    public class MenuElement(string label) : ReactiveObject
    {
        public string Label { get; } = label;
        
        public virtual void OnWindowShow()
        { }
    }
}