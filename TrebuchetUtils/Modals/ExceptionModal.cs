using System;
using System.Windows.Input;
using Avalonia;

namespace TrebuchetUtils.Modals
{
    public class ExceptionModal : BaseModal
    {
        private string _errorMessage = string.Empty;

        public ExceptionModal(Exception exception) : base(650,400,"Exception", "ExceptionModal")
        {
            CloseCommand = new SimpleCommand(OnCloseModal);

            if(Application.Current is not IApplication app) return;
            if (app.HasCrashed) return;
            app.Crash();
            ErrorTitle = "Internal Exception";
            _errorMessage = exception.GetAllExceptions();
            Window.Clipboard?.SetTextAsync(_errorMessage);
        }

        public ExceptionModal(AggregateException exceptions) : base(650,400,"Exception", "ExceptionModal")
        {
            CloseCommand = new SimpleCommand(OnCloseModal);
            
            if(Application.Current is not IApplication app) return;
            if (app.HasCrashed) return;
            app.Crash();
            ErrorTitle = "Internal Exception";
            _errorMessage = exceptions.GetAllExceptions();
            Window.Clipboard?.SetTextAsync(_errorMessage);
        }

        public ICommand CloseCommand { get; private set; }
        public string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }
        public string ErrorTitle { get; set; } = string.Empty;

        public override void Cancel()
        {
            CloseCommand.Execute(this);
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }

        public override void Submit()
        {
            CloseCommand.Execute(this);
        }

        private void OnCloseModal(object? obj)
        {
            Window.Close();
        }
    }
}