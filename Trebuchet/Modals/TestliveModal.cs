using System;
using System.Windows.Input;
using TrebuchetUtils;

namespace Trebuchet.Modals
{
    public class TestliveModal : BaseModal
    {
        private readonly bool _catapult;

        public TestliveModal(bool catapult) : base(400, 400, "Game Build", "TestliveSelection")
        {
            LiveCommand = new SimpleCommand(OnLiveClicked);
            TestLiveCommand = new SimpleCommand(OnTestLiveClicked);
            CloseDisabled = false;
            _catapult = catapult;
        }

        public ICommand LiveCommand { get; private set; }

        public ICommand TestLiveCommand { get; private set; }

        private void OnLiveClicked(object? obj)
        {
            App.OpenApp(false, _catapult);
            Window.Close();
        }

        private void OnTestLiveClicked(object? obj)
        {
            App.OpenApp(true, _catapult);
            Window.Close();
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}