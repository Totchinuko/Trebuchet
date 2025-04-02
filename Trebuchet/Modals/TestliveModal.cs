using System;
using System.Windows.Input;
using TrebuchetUtils;

namespace Trebuchet.Modals
{
    public class TestliveModal : BaseModal
    {
        private readonly App _app;

        public TestliveModal(App app) : base(400, 400, "Game Build", "TestliveSelection")
        {
            _app = app;
            LiveCommand = new SimpleCommand().Subscribe(OnLiveClicked);
            TestLiveCommand = new SimpleCommand().Subscribe(OnTestLiveClicked);
            CloseDisabled = false;
        }

        public ICommand LiveCommand { get; private set; }
        public ICommand TestLiveCommand { get; private set; }
        
        private void OnLiveClicked(object? obj)
        {
            _app.OpenApp(false);
            Window.Close();
        }

        private void OnTestLiveClicked(object? obj)
        {
            _app.OpenApp(true);
            Window.Close();
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}