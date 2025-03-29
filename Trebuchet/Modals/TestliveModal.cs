using System;
using System.Windows.Input;
using TrebuchetUtils;

namespace Trebuchet.Modals
{
    public class TestliveModal : BaseModal
    {
        public TestliveModal() : base(400, 400, "Game Build", "TestliveSelection")
        {
            LiveCommand = new SimpleCommand(OnLiveClicked);
            TestLiveCommand = new SimpleCommand(OnTestLiveClicked);
            CloseDisabled = false;
        }

        public ICommand LiveCommand { get; private set; }
        public ICommand TestLiveCommand { get; private set; }
        
        public bool Result { get; private set; }

        private void OnLiveClicked(object? obj)
        {
            Result = false;
            Window.Close();
        }

        private void OnTestLiveClicked(object? obj)
        {
            Result = true;
            Window.Close();
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}