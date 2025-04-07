using System;
using System.Reactive;
using System.Windows.Input;
using Avalonia;
using ReactiveUI;

namespace TrebuchetUtils.Modals
{
    public class ExceptionModal : BaseModal
    {
        private string _errorMessage = string.Empty;

        public ExceptionModal(Exception exception) : base(650,400,"Exception", "ExceptionModal")
        {
            CloseCommand = ReactiveCommand.Create(OnCloseModal);

            if(Application.Current is not IApplication app) return;
            if (app.HasCrashed) return;
            app.Crash();
            ErrorTitle = "Internal Exception";
            _errorMessage = exception.GetAllExceptions();
            Window.Clipboard?.SetTextAsync(_errorMessage);
        }

        public ExceptionModal(AggregateException exceptions) : base(650,400,"Exception", "ExceptionModal")
        {
            CloseCommand = ReactiveCommand.Create(OnCloseModal);
            
            if(Application.Current is not IApplication app) return;
            if (app.HasCrashed) return;
            app.Crash();
            ErrorTitle = "Internal Exception";
            _errorMessage = exceptions.GetAllExceptions();
            Window.Clipboard?.SetTextAsync(_errorMessage);
        }

        public ReactiveCommand<Unit,Unit> CloseCommand { get; private set; }
        public string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }
        public string ErrorTitle { get; set; } = string.Empty;

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }

        private void OnCloseModal()
        {
            Window.Close();
        }
    }
}