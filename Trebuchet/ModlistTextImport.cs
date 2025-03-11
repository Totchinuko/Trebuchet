using System;
using System.IO;
using TrebuchetGUILib;

namespace Trebuchet
{
    public class ModlistTextImport : BaseModal
    {
        private bool _append = false;
        private bool _canceled = true;
        private bool _export;
        private FileType _fileType = FileType.Json;
        private string _text = string.Empty;

        public ModlistTextImport(string text, bool export, FileType fileType)
        {
            _text = text;
            _export = export;
            _fileType = fileType;
        }

        public bool Append { get => _append; set => _append = value; }

        public ICommand AppendCommand => new SimpleCommand(OnAppend);

        public ICommand ApplyCommand => new SimpleCommand(OnApply);

        public bool Canceled { get => _canceled; set => _canceled = value; }

        public override bool CloseDisabled => false;

        public Visibility ImportVisible => _export ? Visibility.Collapsed : Visibility.Visible;

        public override bool MaximizeDisabled => false;

        public override bool MinimizeDisabled => false;

        public override int ModalHeight => 600;

        public override string ModalTitle => "Mod List";

        public override int ModalWidth => 650;

        public Visibility SaveAsVisible => _export ? Visibility.Visible : Visibility.Collapsed;

        public ICommand SaveCommand => new SimpleCommand(OnSave);

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ModlistTextImport"];

        public string Text { get => _text; set => _text = value; }

        public override void OnWindowClose()
        {
        }

        private void OnAppend(object? obj)
        {
            if (_export) return;
            _append = true;
            _canceled = false;
            _window.Close();
        }

        private void OnApply(object? obj)
        {
            if (_export) return;
            _canceled = false;
            _window.Close();
        }

        private void OnSave(object? obj)
        {
            if (!_export) return;
            if (string.IsNullOrEmpty(_text)) return;

            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.DefaultExt = _fileType.extention;
            dialog.AddExtension = true;
            dialog.FileName = "Untitled";
            dialog.Filter = $"{_fileType.name} (*.{_fileType.extention})|*.{_fileType.extention}";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Windows.Forms.DialogResult result = dialog.ShowDialog(NativeWindow.GetIWin32Window(_window));
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = Path.GetFullPath(dialog.FileName);
                File.WriteAllText(path, _text);
            }
        }
    }
}