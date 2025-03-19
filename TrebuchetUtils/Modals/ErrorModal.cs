using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;

namespace TrebuchetUtils.Modals
{
    public class ErrorModal : BaseModal
    {
        public ErrorModal(string title, string message) : base(650, 200, "Error", "ErrorModal")
        {
            CloseCommand = new SimpleCommand(OnCloseModal);

            ErrorMessage = message;
            ErrorTitle = title;
        }

        public ICommand CloseCommand { get; private set; }
        public string ErrorMessage { get; }
        public string ErrorTitle { get; }

        public static async Task ShowError(string error, string title = "Error")
        {
            if(Dispatcher.UIThread.CheckAccess())
            {
                await new ErrorModal(title, error).OpenDialogueAsync();
            }
            else
                await Dispatcher.UIThread.InvokeAsync(() => ShowError(error, title));
        }

        private void OnCloseModal(object? obj)
        {
            Window.Close();
        }

        public override void Submit()
        {
            CloseCommand.Execute(this);
        }

        public override void Cancel()
        {
            CloseCommand.Execute(this);
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}