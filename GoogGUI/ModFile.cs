using Goog;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace GoogGUI
{
    internal class ModFile : INotifyPropertyChanged
    {
        private string _authorName = string.Empty;
        private SteamPublishedFile? _file;
        private bool _fileExists = false;
        private bool _isID;
        private bool _isValid;
        private DateTime _lastModified;
        private DateTime _lastUpdate;
        private string _mod = string.Empty;

        public ModFile(string mod, bool fileExists, DateTime lastModified = default)
        {
            _mod = mod;
            _isID = long.TryParse(_mod, out _);
            _isValid = _isID || File.Exists(_mod);
            _fileExists = fileExists;
            _lastModified = lastModified;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string AuthorName
        {
            get
            {
                if (!string.IsNullOrEmpty(_authorName))
                    return _authorName;
                if (_file != null)
                    return _file.creator;
                return "Unknown";
            }
            set
            {
                _authorName = value;
                OnPropertyChanged("AuthorName");
            }
        }

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

        public SteamPublishedFile? PublishedFile => _file;

        public Brush StatusColor => GetStatusBrush();

        public string StatusTooltip => GetStatusText();

        public void SetManifest(SteamPublishedFile file)
        {
            if (!_isID)
                throw new Exception("Cannot set a manifest on a local mod.");
            _file = file;
            _lastUpdate = Tools.UnixTimeStampToDateTime(_file.timeUpdated);
            OnPropertyChanged("ModName");
            OnPropertyChanged("LastUpdate");
            OnPropertyChanged("StatusColor");
            OnPropertyChanged("StatusTooltip");
        }

        protected virtual Brush GetStatusBrush()
        {
            if (!_fileExists) return (Brush)Application.Current.Resources["GDimRed"];
            if (_file == null) return (Brush)Application.Current.Resources["GDimBlue"];
            if (_lastUpdate < _lastModified) return (Brush)Application.Current.Resources["GDimGreen"];
            return (Brush)Application.Current.Resources["GDimYellow"];
        }

        protected virtual string GetStatusText()
        {
            if (!_fileExists) return "Missing";
            if (_file == null) return "Found";
            if (_lastUpdate < _lastModified) return "Up to Date";
            return "Update available";
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}