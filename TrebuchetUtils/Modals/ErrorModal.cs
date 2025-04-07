using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using ReactiveUI;

namespace TrebuchetUtils.Modals
{
    public class ErrorModal : BaseModal
    {
        public ErrorModal(string title, string message) : base(650, 200, "Error", "ErrorModal")
        {
            CloseCommand = ReactiveCommand.Create(OnCloseModal);

            ErrorMessage = message;
            ErrorTitle = title;
        }

        public ReactiveCommand<Unit,Unit> CloseCommand { get; }
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

        private void OnCloseModal()
        {
            Window.Close();
        }

        public override void Submit()
        {
            CloseCommand.Execute();
        }

        public override void Cancel()
        {
            CloseCommand.Execute();
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}