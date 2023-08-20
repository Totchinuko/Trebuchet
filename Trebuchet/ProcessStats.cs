using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Trebuchet
{
    public class ProcessStats : INotifyPropertyChanged
    {
        protected const string CPUFormat = "{0}%";
        protected const string MemoryFormat = "{0}MB (Peak {1}MB)";
        protected ProcessData _process;
        private int _cpuUsage = 0;

        private long _memoryConsumption = 0;
        private long _peakMemoryConsumption = 0;
        private CancellationTokenSource? _source;
        private DateTime _start;

        private DispatcherTimer _timer;
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

        public int PID => _process.pid;

        public bool Running => !_process.IsEmpty;

        public string Uptime => _process.IsEmpty ? string.Empty : (DateTime.UtcNow - _start).ToString("d'd.'h'h:'m'm:'s's'");

        public virtual void StartStats(ProcessData process, string processName)
        {
            if (!_process.IsEmpty) throw new Exception("Stats already have a process.");

            _process = process;
            _start = _process.start;

            _source = new CancellationTokenSource();
            Task.Run(() => RunCounters(_process.pid, processName, _source.Token), _source.Token);

            _timer.Start();
            OnPropertyChanged("Running");
            OnPropertyChanged("PID");
        }

        public virtual void StopStats()
        {
            _process = ProcessData.Empty;
            _source?.Cancel();
            _source?.Dispose();
            _source = null;
            _timer.Stop();
            _peakMemoryConsumption = 0;
            OnPropertyChanged("Running");
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnTick(object? sender, EventArgs e)
        {
            if (_process.IsEmpty) return;

            lock (theLock)
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
            PerformanceCounter memoryConsumptionCounter = await Task.Run(() => new PerformanceCounter("Process V2", "Working Set", processName + ":" + processID));
            PerformanceCounter cpuUsageCounter = await Task.Run(() => new PerformanceCounter("Process V2", "% Processor Time", processName + ":" + processID));

            while (!token.IsCancellationRequested)
            {
                if (Tools.GetProcess(processID).IsEmpty)
                    break;

                long memory = 0;
                int cpu = 0;
                try
                {
                    memory = (long)(memoryConsumptionCounter?.NextValue() ?? 0f);
                    cpu = (int)(cpuUsageCounter?.NextValue() / Environment.ProcessorCount ?? 0f);
                }
                catch { break; }

                lock (theLock)
                {
                    _memoryConsumption = memory;
                    _cpuUsage = cpu;
                }

                await Task.Delay(900);
            }

            memoryConsumptionCounter?.Dispose();
            cpuUsageCounter?.Dispose();
        }
    }
}