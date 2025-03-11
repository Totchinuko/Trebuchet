using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Templates;

namespace TrebuchetUtils
{
    public abstract class BaseModal
    {
        private readonly ModalWindow _window;
        protected BaseModal(int width, int height, string title, DataTemplate template)
        {
            Window? owner;
            WindowStartupLocation location;
            // User should not be able to go back on the main window as long as the modal was not removed..
            // But in some cases we want to be to show it without a main window open
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new ApplicationException("This is a desktop application");
                
            if (desktop.MainWindow is IShownWindow mainWin && mainWin.WasShown())
            {
                owner = desktop.MainWindow;
                location = WindowStartupLocation.CenterOwner;
            }
            else
            {
                location = WindowStartupLocation.CenterScreen;
                owner = null;
            }
            
            ModalTitle = title;
            Template = template;
            _window = new ModalWindow(this, owner)
            {
                Height = height,
                Width = width,
                WindowStartupLocation = location
            };
        }

        public bool CloseDisabled { get; protected set; } = true;

        public bool MaximizeDisabled { get; protected set; } = true;

        public bool MinimizeDisabled { get; protected set; } = true;

        public string ModalTitle { get; }

        public bool CanResize { get; protected set; } = false;
        public DataTemplate Template { get; }
        public Window Window => _window;

        public virtual void Cancel()
        {
        }

        public void Close() => _window.Close();

        public abstract void OnWindowClose();

        public void SetNoOwner()
        {
            _window.SetOwner(null);
            _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        public void SetOwner(Window owner)
        {
            if (owner is IShownWindow shown && shown.WasShown())
            {
                _window.SetOwner(owner);
                _window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else throw new ArgumentException(null, nameof(owner));
        }

        public bool SetOwner<T>() where T : class
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new ApplicationException("This is a desktop application");
            foreach (var win in desktop.Windows)
            {
                if (win is not T) continue;
                SetOwner(win);
                return true;
            }
            return false;
        }

        public void Show() => _window.PopDialog(false);

        public void ShowDialog() => _window.PopDialog();

        public virtual void Submit()
        {
        }
    }
}