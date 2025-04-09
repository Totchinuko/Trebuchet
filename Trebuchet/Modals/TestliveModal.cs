using System;
using System.Reactive;
using ReactiveUI;
using Trebuchet.Assets;
using TrebuchetUtils;

namespace Trebuchet.Modals
{
    public class TestliveModal : BaseModal
    {
        private readonly App _app;

        public TestliveModal(App app) : base(400, 400, Resources.GameBuild, "TestliveSelection")
        {
            _app = app;
            LiveCommand = ReactiveCommand.Create(OnLiveClicked);
            TestLiveCommand = ReactiveCommand.Create(OnTestLiveClicked);
            CloseDisabled = false;
        }

        public ReactiveCommand<Unit, Unit> LiveCommand { get; }
        public ReactiveCommand<Unit, Unit> TestLiveCommand { get; }
        
        private void OnLiveClicked()
        {
            _app.OpenApp(false);
            Window.Close();
        }

        private void OnTestLiveClicked()
        {
            _app.OpenApp(true);
            Window.Close();
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}