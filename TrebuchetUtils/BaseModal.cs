using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;

namespace TrebuchetUtils
{
    public abstract class BaseModal
    {
        private readonly ModalWindow _window;
        private readonly string _template;
        protected BaseModal(int width, int height, string title, string template)
        {
            ModalTitle = title;
            _template = template;
            _window = new ModalWindow()
            {
                Height = height,
                Width = width,
            };
            _window.SetModal(this);

            _window.WindowClosed += OnWindowClose;
        }

        public bool CloseDisabled { get; protected set; } = true;

        public bool MaximizeDisabled { get; protected set; } = true;

        public bool MinimizeDisabled { get; protected set; } = true;

        public string ModalTitle { get; }

        public bool CanResize { get; protected set; } = false;
        public IDataTemplate Template {
            get
            {
                if(Application.Current == null) throw new Exception("Application.Current is null");

                if (Application.Current.Resources.TryGetResource(_template, Application.Current.ActualThemeVariant,
                        out var resource) && resource is IDataTemplate template)
                {
                    return template;
                }

                throw new Exception($"Template {_template} not found");
            }
        }
        public Window Window => _window;

        public virtual void Cancel()
        {
        }

        public void Close() => _window.Close();

        protected abstract void OnWindowClose(object? sender, EventArgs e);

        public void Open() => _window.OpenDialogue();
        
        public void OpenDialogue(Window window) => _window.OpenDialogue(window);

        public void OpenDialogue()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new ApplicationException("This is a desktop application");
            
            if (desktop.MainWindow is IShownWindow { WasShown: true })
            {
                _window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _window.OpenDialogue(desktop.MainWindow);
            }
            else
            {
                _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _window.OpenDialogue();
            }
        }

        public void OpenDialogue<T>() where T : Window
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new ApplicationException("This is a desktop application");
            foreach (var win in desktop.Windows)
            {
                if (win is not T) continue;
                _window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _window.OpenDialogue(win);
                return;
            }
            _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _window.OpenDialogue();
        }

        public virtual void Submit()
        {
        }
    }
}