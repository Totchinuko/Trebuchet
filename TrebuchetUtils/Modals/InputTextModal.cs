using System;
using System.Windows.Input;
using Avalonia.Layout;

namespace TrebuchetUtils.Modals
{
    public class InputTextModal : BaseModal
    {
        private string? _text = string.Empty;
        private bool _validated;

        public InputTextModal(string buttonLabel, string watermark = "", bool acceptReturn = false) : base(650, 200, "Text", "InputTextModal")
        {
            Watermark = watermark;
            ValidateCommand = new SimpleCommand(OnValidate);
            ButtonLabel = buttonLabel;
            AcceptReturn = acceptReturn;
            CloseDisabled = false;
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
        public string ButtonLabel { get; set; }

        public double FieldHeight => AcceptReturn ? 120.0 : 32.0;
        public VerticalAlignment FieldVerticalAlignment => AcceptReturn ? VerticalAlignment.Top : VerticalAlignment.Center;
        public string Watermark { get; private set; }
        public int MaxLength { get; private set; } = -1;
        public string? Text { get => _text; set => _text = value; }

        public ICommand ValidateCommand { get; private set; }


        protected override void OnWindowClose(object? sender, EventArgs e)
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
            Window.Close();
        }
    }
}