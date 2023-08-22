using System.Windows;
using System.Windows.Input;

namespace Trebuchet
{
    public class TestliveModal : BaseModal
    {
        private bool _catapult;
        private bool _madeASelection;

        public TestliveModal(bool catapult) : base()
        {
            LiveCommand = new SimpleCommand(OnLiveClicked);
            TestLiveCommand = new SimpleCommand(OnTestLiveClicked);
            _catapult = catapult;
        }

        public override bool CloseDisabled => false;

        public ICommand LiveCommand { get; private set; }

        public override int ModalHeight => 400;

        public override string ModalTitle => "Game Build";

        public override int ModalWidth => 400;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["TestliveSelection"];

        public ICommand TestLiveCommand { get; private set; }

        public override void OnWindowClose()
        {
            if (!_madeASelection)
                Application.Current.Shutdown();
        }

        private void OnLiveClicked(object? obj)
        {
            _madeASelection = true;
            App.OpenApp(false, _catapult);
            _window.Close();
        }

        private void OnTestLiveClicked(object? obj)
        {
            _madeASelection = true;
            App.OpenApp(true, _catapult);
            _window.Close();
        }
    }
}