using Goog;
using System;
using System.ComponentModel;
using System.IO;

namespace GoogGUI
{
    internal class ModFile : INotifyPropertyChanged
    {
        private SteamPublishedFile? _file;
        private bool _isID;
        private bool _isValid;
        private string _mod = string.Empty;

        public ModFile(string mod)
        {
            _mod = mod;
            _isID = long.TryParse(_mod, out _);
            _isValid = _isID || File.Exists(_mod);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsID => _isID;

        public bool IsValid => _isValid;

        public string Mod => _mod;

        public string ModName
        {
            get
            {
                if (_file != null)
                    return _file.title;
                if (_isID)
                    return _mod;
                return Path.GetFileName(_mod);
            }
        }

        public string LastUpdate
        {
            get
            {
                if (_file == null)
                    return string.Empty;
                DateTime date = Tools.UnixTimeStampToDateTime(_file.timeUpdated);
                return date.ToShortDateString() + " " + date.ToShortTimeString();
            }
        }

        public void SetManifest(SteamPublishedFile file)
        {
            if (!_isID)
                throw new Exception("Cannot set a manifest on a local mod.");
            _file = file;
            OnPropertyChanged("ModName");
            OnPropertyChanged("LastUpdate");
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}