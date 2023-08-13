using Goog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    public class ProcessStats : INotifyPropertyChanged
    {
        protected const string MemoryFormat = "{0}MB (Peak {1}MB)";
        protected const string CPUFormat = "{0}%";

        protected Process? _process;
        private int _cpuUsage = 0;
        private PerformanceCounter? _cpuUsageCounter;

        private long _memoryConsumption = 0;
        private PerformanceCounter? _memoryConsumptionCounter;
        private long _peakMemoryConsumption = 0;
        private DateTime _start;

        private DispatcherTimer _timer;
        private CancellationTokenSource? _source;

        private object theLock = new object();

        public ProcessStats()
        {
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTick, Application.Current.Dispatcher);
            CpuUsage = string.Empty;
            MemoryConsumption = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CpuUsage { get; private set; }

        public string MemoryConsumption { get; private set; }

        public string Uptime => _process == null ? string.Empty : (DateTime.UtcNow - _start).ToString("d'd.'h'h:'m'm:'s's'");

        public bool Running => _process != null;

        public virtual void SetProcess(Process process, string processName)
        {
            if (_process != null) throw new Exception("Stats already have a process.");

            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;

            _start = DateTime.UtcNow;

            _source = new CancellationTokenSource();
            Task.Run(() => RunCounters(process.Id, processName, _source.Token));

            _timer.Start();
            OnPropertyChanged("Running");
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            _process = null;
            _source?.Cancel();
            _timer.Stop();
            OnPropertyChanged("Running");
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnTick(object? sender, EventArgs e)
        {
            if (_process == null) return;

            lock(theLock)
            {
                _peakMemoryConsumption = Math.Max(_memoryConsumption, _peakMemoryConsumption);
                CpuUsage = string.Format(CPUFormat, _cpuUsage.ToString());
                MemoryConsumption = string.Format(MemoryFormat, (_memoryConsumption / 1024 / 1024), (_peakMemoryConsumption / 1024 / 1024));
            }

            OnPropertyChanged("MemoryConsumption");
            OnPropertyChanged("PeakMemoryConsumption");
            OnPropertyChanged("CpuUsage");
            OnPropertyChanged("Uptime");
        }

        protected virtual async Task RunCounters(int processID, string processName, CancellationToken token)
        {
            _memoryConsumptionCounter = await Task.Run(() => new PerformanceCounter("Process V2", "Working Set", processName + ":" + processID));
            _cpuUsageCounter = await Task.Run(() => new PerformanceCounter("Process V2", "% Processor Time", processName + ":" + processID));

            while(!token.IsCancellationRequested)
            {
                long memory = (long)(_memoryConsumptionCounter?.NextValue() ?? 0f);
                int cpu = (int)(_cpuUsageCounter?.NextValue() / Environment.ProcessorCount ?? 0f);

                lock (theLock)
                {
                    _memoryConsumption = memory;
                    _cpuUsage = cpu;
                }

                await Task.Delay(1000);
            }

            _memoryConsumptionCounter?.Dispose();
            _cpuUsageCounter?.Dispose();
            _memoryConsumptionCounter = null;
            _cpuUsageCounter = null;
        }
    }
}