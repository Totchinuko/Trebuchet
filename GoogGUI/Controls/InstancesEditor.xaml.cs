using GoogLib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogGUI.Controls
{
    /// <summary>
    /// Interaction logic for ServerInstances.xaml
    /// </summary>
    public partial class InstancesEditor : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty InstancesProperty = DependencyProperty.Register(
            "Instances",
            typeof(TrulyObservableCollection<ObservableServerInstance>),
            typeof(InstancesEditor),
            new PropertyMetadata(
                new TrulyObservableCollection<ObservableServerInstance>() { new ObservableServerInstance(new ServerInstance()) })
            );

        public static readonly DependencyProperty ProfilesProperty = DependencyProperty.Register(
            "Profiles",
            typeof(ObservableCollection<string>),
            typeof(InstancesEditor),
            new PropertyMetadata(
                default(ObservableCollection<string>))
            );

        public InstancesEditor()
        {
            DeleteCommand = new SimpleCommand(OnInstanceDelete);
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand DeleteCommand { get; private set; }

        public TrulyObservableCollection<ObservableServerInstance> Instances
        {
            get => (TrulyObservableCollection<ObservableServerInstance>)GetValue(InstancesProperty);
            set => SetValue(InstancesProperty, value);
        }

        public ObservableCollection<string> Profiles
        {
            get => (ObservableCollection<string>)GetValue(ProfilesProperty);
            set => SetValue(ProfilesProperty, value);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Instances == null)
                Instances = new TrulyObservableCollection<ObservableServerInstance> { new ObservableServerInstance(new ServerInstance()) };
            Instances.Add(new ObservableServerInstance(new ServerInstance()));
        }

        private void OnInstanceDelete(object? obj)
        {
            if (Instances.Count <= 1) return;
            if (obj is ObservableServerInstance instance)
                Instances.Remove(instance);
            InstanceList.ItemsSource = Instances;
        }
    }
}