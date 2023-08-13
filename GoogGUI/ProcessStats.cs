using Goog;
using GoogLib;
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

        protected ProcessData _process;
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
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(1000), DispatcherPriority.Background, OnTick, Application.Current.Dispatcher);
            CpuUsage = string.Empty;
            MemoryConsumption = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CpuUsage { get; private set; }

        public string MemoryConsumption { get; private set; }

        public string Uptime => _process.IsEmpty ? string.Empty : (DateTime.UtcNow - _start).ToString("d'd.'h'h:'m'm:'s's'");

        public bool Running => !_process.IsEmpty;

        public virtual void StartStats(ProcessData process, string processName)
        {
            if (!_process.IsEmpty) throw new Exception("Stats already have a process.");

            _process = process;
            _start = _process.start;

            _source = new CancellationTokenSource();
            Task.Run(() => RunCounters(_process.pid, Path.GetFileNameWithoutExtension(process.filename), _source.Token));

            _timer.Start();
            OnPropertyChanged("Running");
        }

        public virtual void StopStats()
        {
            _process = ProcessData.Empty;
            _source?.Cancel();
            _source?.Dispose();
            _source = null;
            _timer.Stop();
            OnPropertyChanged("Running");
        }


        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnTick(object? sender, EventArgs e)
        {
            if (_process.IsEmpty) return;

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

                await Task.Delay(900);
            }

            _memoryConsumptionCounter?.Dispose();
            _cpuUsageCounter?.Dispose();
            _memoryConsumptionCounter = null;
            _cpuUsageCounter = null;
        }
    }
}