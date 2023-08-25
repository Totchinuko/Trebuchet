using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TrebuchetLib;

namespace Trebuchet
{
    public class ProcessStats : INotifyPropertyChanged
    {
        protected const string CPUFormat = "{0}%";
        protected const string MemoryFormat = "{0}MB (Peak {1}MB)";
        private int _cpuUsage = 0;

        private ProcessDetails? _details;
        private object _lock = new object();
        private long _memoryConsumption = 0;
        private long _peakMemoryConsumption = 0;
        private CancellationTokenSource? _source;
        private DispatcherTimer _timer;

        public ProcessStats()
        {
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(1000), DispatcherPriority.Background, OnTick, Application.Current.Dispatcher);
            CpuUsage = string.Empty;
            MemoryConsumption = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CpuUsage { get; private set; }

        public string MemoryConsumption { get; private set; }

        public int PID => (int)(_details?.PID ?? 0);

        public string PlayerCount { get; private set; } = string.Empty;

        public string ProcessStatus => _details?.State.ToString() ?? string.Empty;

        public bool Running => _details?.State.IsRunning() ?? false;

        public string Uptime => _details == null ? string.Empty : (DateTime.UtcNow - _details.StartUtc).ToString("d'd.'h'h:'m'm:'s's'");

        public void SetDetails(ProcessDetails details)
        {
            _details = details;
            OnPropertyChanged(nameof(ProcessStatus));
        }

        public virtual void StartStats(ProcessDetails details)
        {
            if (_source != null) throw new Exception("Stats already have a process.");

            SetDetails(details);

            _source = new CancellationTokenSource();
            if (App.Config.DisplayProcessPerformance)
                Task.Run(() => RunCounters(PID, details.ProcessName, _source.Token), _source.Token);
            else
            {
                MemoryConsumption = string.Empty;
                CpuUsage = string.Empty;
                OnPropertyChanged(nameof(MemoryConsumption));
                OnPropertyChanged(nameof(CpuUsage));
            }

            _timer.Start();
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(PID));
            OnPropertyChanged(nameof(ProcessStatus));
        }

        public virtual void StopStats(ProcessDetails details)
        {
            SetDetails(details);
            _source?.Cancel();
            _source?.Dispose();
            _source = null;
            _timer.Stop();
            _peakMemoryConsumption = 0;
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(ProcessStatus));
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnTick(object? sender, EventArgs e)
        {
            if (!Running) return;

            if (App.Config.DisplayProcessPerformance)
                lock (_lock)
                {
                    _peakMemoryConsumption = Math.Max(_memoryConsumption, _peakMemoryConsumption);
                    CpuUsage = string.Format(CPUFormat, _cpuUsage.ToString());
                    MemoryConsumption = string.Format(MemoryFormat, (_memoryConsumption / 1024 / 1024), (_peakMemoryConsumption / 1024 / 1024));
                }

            if (_details is ProcessServerDetails serverDetails)
            {
                PlayerCount = $"{serverDetails.Players}/{serverDetails.MaxPlayers}";
                OnPropertyChanged(nameof(PlayerCount));
            }

            OnPropertyChanged(nameof(MemoryConsumption));
            OnPropertyChanged(nameof(CpuUsage));
            OnPropertyChanged(nameof(Uptime));
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

                lock (_lock)
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