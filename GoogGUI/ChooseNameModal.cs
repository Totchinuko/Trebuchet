using System;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class ChooseNameModal : BaseModal
    {
        private string _buttonLabel = string.Empty;
        private string _name = string.Empty;
        private bool _validated;

        public ChooseNameModal(string buttonLabel, string value)
        {
            ValidateCommand = new SimpleCommand(OnValidate);
            _buttonLabel = buttonLabel;
            _name = value;
        }

        public string ButtonLabel { get => _buttonLabel; set => _buttonLabel = value; }

        public override int ModalHeight => 200;

        public override string ModalTitle => "Name";

        public override int ModalWidth => 650;

        public override bool CloseDisabled => false;

        public string Name { get => _name; set => _name = value; }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ChooseNameModal"];

        public ICommand ValidateCommand { get; private set; }

        public override void OnWindowClose()
        {
            if (!_validated)
                _name = string.Empty;
        }

        private void OnValidate(object? obj)
        {
            if (string.IsNullOrEmpty(_name))
                return;
            _validated = true;
            _window.Close();
        }

        public override void Submit()
        {
            OnValidate(this);
        }
    }
}