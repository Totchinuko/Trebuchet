using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;

namespace TrebuchetUtils.Modals
{
    public class ErrorModal : BaseModal
    {
        public ErrorModal(string title, string message) : base(650, 200, "Error", null)
        {
            CloseCommand = new SimpleCommand(OnCloseModal);

            ErrorMessage = message;
            ErrorTitle = title;
        }

        public ICommand CloseCommand { get; private set; }
        public string ErrorMessage { get; }
        public string ErrorTitle { get; }

        public static async void ShowError(string error, string title = "Error")
        {
            if(Application.Current.Dispatcher.CheckAccess())
            {
                new ErrorModal(title, error, exit).ShowDialog();
            }
            else
                await Application.Current.Dispatcher.InvokeAsync(() => ShowError(error, title, exit));
        }

        public override void OnWindowClose()
        {
        }

        private void OnCloseModal(object? obj)
        {
            Window?.Close();
        }

        public override void Submit()
        {
            CloseCommand?.Execute(this);
        }

        public override void Cancel()
        {
            CloseCommand?.Execute(this);
        }
    }
}