using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogGUI
{
    public class TaskBlocker : INotifyPropertyChanged
    {
        private string _description = string.Empty;
        private CancellationTokenSource? _source;
        private Task? _task;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand CancelCommand => new SimpleCommand((x) => _source?.Cancel());

        public string Description 
        { 
            get => _description; 
            set
            {
                _description = value;
                OnPropertyChanged("Description");
            }
        }

        public bool IsAvailable => _task == null;

        public void Release()
        {
            _task = null;
            _source = null;
            Description = string.Empty;
            OnPropertyChanged("IsAvailable");
        }

        public void Set(Task task, string description, CancellationTokenSource source)
        {
            if (_task != null)
                throw new Exception("Cannot set a new blocking task while one is already running");

            _task = task;
            _source = source;
            Description = description;
            OnPropertyChanged("IsAvailable");
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}