using Goog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    public class ProcessStats : INotifyPropertyChanged
    {
        protected const string MemoryFormat = "Memory: {0}MB (Max {1}MB)";
        protected const string CPUFormat = "CPU: {0}%";

        protected Process? _process;
        private int _cpuUsage = 0;
        private PerformanceCounter? _cpuUsageCounter;

        private long _memoryConsumption = 0;
        private PerformanceCounter? _memoryConsumptionCounter;
        private long _peakMemoryConsumption = 0;
        private DateTime _start;

        private DispatcherTimer _timer;

        public ProcessStats()
        {
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTick, Application.Current.Dispatcher);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CpuUsage => string.Format(CPUFormat, _cpuUsage.ToString());

        public string MemoryConsumption => string.Format(MemoryFormat, (_memoryConsumption / 1024 / 1024), (_peakMemoryConsumption / 1024 / 1024));

        public string Uptime => _process == null ? string.Empty : (DateTime.UtcNow - _start).ToString("c");

        public virtual void SetProcess(Process process)
        {
            if (_process != null) throw new Exception("Stats already have a process.");

            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;

            string processName = Path.GetFileNameWithoutExtension(_process.StartInfo.FileName);

            Task.Run(() => _memoryConsumptionCounter = new PerformanceCounter("Process V2", "Working Set", processName + ":" + process.Id)).ContinueWith(OnMemoryCounterCreated);
            Task.Run(() => _cpuUsageCounter = new PerformanceCounter("Process V2", "% Processor Time", processName + ":" + process.Id)).ContinueWith(OnCPUCounterCreated);

            _start = DateTime.UtcNow;

            _timer.Start();
        }

        private void OnCPUCounterCreated(Task<PerformanceCounter> task)
        {
            _cpuUsageCounter = task.Result;
        }

        private void OnMemoryCounterCreated(Task<PerformanceCounter> task)
        {
            _memoryConsumptionCounter = task.Result;
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            _process = null;
            _memoryConsumptionCounter?.Dispose();
            _cpuUsageCounter?.Dispose();
            _memoryConsumptionCounter = null;
            _cpuUsageCounter = null;

            _memoryConsumption = _peakMemoryConsumption = 0;
            _cpuUsage = 0;
            _timer.Stop();
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnTick(object? sender, EventArgs e)
        {
            if (_process == null) return;

            _memoryConsumption = (long)(_memoryConsumptionCounter?.NextValue() ?? 0f);
            _peakMemoryConsumption = Math.Max(_memoryConsumption, _peakMemoryConsumption);

            _cpuUsage = (int)(_cpuUsageCounter?.NextValue() / Environment.ProcessorCount ?? 0f);

            OnPropertyChanged("MemoryConsumption");
            OnPropertyChanged("PeakMemoryConsumption");
            OnPropertyChanged("CpuUsage");
        }
    }
}