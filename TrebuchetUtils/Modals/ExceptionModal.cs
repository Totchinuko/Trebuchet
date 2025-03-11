using System;
using System.Media;
using TrebuchetUtils;

namespace TrebuchetUtils.Modals
{
    public class ExceptionModal : BaseModal
    {
        private string _errorMessage = string.Empty;
        private string _errorTitle = string.Empty;

        public ExceptionModal(Exception exception) : base()
        {
            CloseCommand = new SimpleCommand(OnCloseModal);
            if (TrebuchetBaseApp.HasCrashed) return;
            TrebuchetBaseApp.Crash();
            _errorTitle = "Internal Exception";
            _errorMessage = exception.GetAllExceptions();
            Clipboard.SetText(_errorMessage);
            if (!((TrebuchetBaseApp)Application.Current).IsShutingDown)
                SystemSounds.Exclamation.Play();
        }

        public ExceptionModal(AggregateException exceptions) : base()
        {
            CloseCommand = new SimpleCommand(OnCloseModal);
            if (TrebuchetBaseApp.HasCrashed) return;
            TrebuchetBaseApp.Crash();
            _errorTitle = "Internal Exception";
            _errorMessage = exceptions.GetAllExceptions();
            Clipboard.SetText(_errorMessage);
            if (!((TrebuchetBaseApp)Application.Current).IsShutingDown)
                SystemSounds.Exclamation.Play();
        }

        public ICommand CloseCommand { get; private set; }
        public string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }
        public string ErrorTitle { get => _errorTitle; set => _errorTitle = value; }
        protected override int ModalHeight => 400;

        public override string ModalTitle => "Exception";

        protected override int ModalWidth => 650;
        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ExceptionModal"];

        public override void Cancel()
        {
            CloseCommand?.Execute(this);
        }

        public override void OnWindowClose()
        {
            Application.Current.Shutdown();
        }

        public override void Submit()
        {
            CloseCommand?.Execute(this);
        }

        private void OnCloseModal(object? obj)
        {
            Application.Current.Shutdown();
            _window?.Close();
        }
    }
}