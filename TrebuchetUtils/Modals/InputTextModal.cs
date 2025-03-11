using System;
using System.Windows;
using System.Windows.Input;

namespace TrebuchetUtils.Modals
{
    public class InputTextModal : BaseModal
    {
        private string _buttonLabel = string.Empty;
        private string? _text = string.Empty;
        private bool _validated;

        public InputTextModal(string buttonLabel, string watermark = "", bool acceptReturn = false)
        {
            Watermark = watermark;
            ValidateCommand = new SimpleCommand(OnValidate);
            _buttonLabel = buttonLabel;
            AcceptReturn = acceptReturn;
        }

        public void SetMaxLength(int maxLength)
        {
            MaxLength = maxLength;
        }

        public void SetWatermark(string watermark)
        {
            Watermark = watermark;
        }

        public void SetValue(string value)
        {
            _text = value;
        }

        public bool AcceptReturn { get; private set; }
        public string ButtonLabel { get => _buttonLabel; set => _buttonLabel = value; }

        public override bool CloseDisabled => false;
        public double FieldHeight => AcceptReturn ? 120.0 : 32.0;
        public VerticalAlignment fieldVerticalAlignement => AcceptReturn ? VerticalAlignment.Top : VerticalAlignment.Center;
        protected override int ModalHeight => 200;
        public override string ModalTitle => "Name";
        protected override int ModalWidth => 650;
        public string Watermark { get; private set; }
        public int MaxLength { get; private set; } = -1;
        public string? Text { get => _text; set => _text = value; }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["InputTextModal"];

        public ICommand ValidateCommand { get; private set; }

        public override void OnWindowClose()
        {
            if (!_validated)
                _text = null;
        }

        public override void Submit()
        {
            OnValidate(this);
        }

        private void OnValidate(object? obj)
        {
            if (string.IsNullOrEmpty(_text))
                return;
            _validated = true;
            _window.Close();
        }
    }
}