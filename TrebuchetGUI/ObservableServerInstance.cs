using System.ComponentModel;

namespace Trebuchet
{
    public class ObservableServerInstance : INotifyPropertyChanged
    {
        private ServerInstance _instance;

        public ObservableServerInstance(ServerInstance instance)
        {
            _instance = instance;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ServerInstance Instance { get => _instance; private set => _instance = value; }

        public string Profile
        {
            get => _instance.Profile;
            set
            {
                if (_instance.Profile == value) return;
                _instance.Profile = value;
                OnPropertyChanged("Profile");
            }
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}