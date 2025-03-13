using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using TrebuchetLib;

namespace Trebuchet
{
    public class ProcessStatsLight : INotifyPropertyChanged, IProcessStats
    {
        protected const string CpuFormat = "{0}%";
        protected const string MemoryFormat = "{0}MB (Peak {1}MB)";
        private static DateTime? _previousCpuStartTime;
        private static long? _previousTotalProcessorTime;
        private ProcessDetails? _details;
        private long _peakMemoryConsumption;
        private Process? _process;
        private readonly DispatcherTimer _timer;

        public ProcessStatsLight()
        {
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(1000), DispatcherPriority.Background, OnTick);
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

        public void StartStats(ProcessDetails details)
        {
            if (!TryGetProcess((int)details.PID, out Process? process)) return;
            _process = process;

            SetDetails(details);
            _previousCpuStartTime = DateTime.UtcNow;
            _previousTotalProcessorTime = process.TotalProcessorTime.Ticks;
            _timer.Start();
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(PID));
            OnPropertyChanged(nameof(ProcessStatus));
        }

        public void StopStats(ProcessDetails details)
        {
            SetDetails(details);
            _process = null;
            _timer.Stop();
            _peakMemoryConsumption = 0;
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(ProcessStatus));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnTick(object? sender, EventArgs e)
        {
            if (!Running) return;

            if (App.Config.DisplayProcessPerformance)
            {
                long memoryConsumption = GetMemoryUsageForProcess();
                CpuUsage = string.Format(CpuFormat, GetCpuUsageForProcess().ToString("N2"));
                _peakMemoryConsumption = Math.Max(memoryConsumption, _peakMemoryConsumption);
                MemoryConsumption = string.Format(MemoryFormat, (memoryConsumption / 1024 / 1024), (_peakMemoryConsumption / 1024 / 1024));
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

        private double GetCpuUsageForProcess()
        {
            if (_process == null) return 0;

            // If no start time set then set to now
            if (_previousCpuStartTime == null || _previousTotalProcessorTime == null)
                throw new Exception("Time values have not been initialized properly");

            var currentCpuStartTime = DateTime.UtcNow;
            var currentCpuUsage = _process.TotalProcessorTime.Ticks;

            var cpuUsedMs = (currentCpuUsage - (long)_previousTotalProcessorTime);
            var totalMsPassed = (currentCpuStartTime - (DateTime)_previousCpuStartTime).Ticks;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            // Set previous times.
            _previousCpuStartTime = currentCpuStartTime;
            _previousTotalProcessorTime = currentCpuUsage;

            return cpuUsageTotal * 100.0;
        }

        private long GetMemoryUsageForProcess()
        {
            if (_details == null) return 0;
            if (!TryGetProcess((int)_details.PID, out Process? process)) return 0;
            var workingSet64 = process.WorkingSet64;
            var privateMemorySize64 = process.PrivateMemorySize64;
            var virtualMemorySize64 = process.VirtualMemorySize64;

            return workingSet64;
        }

        private bool TryGetProcess(int pid, [NotNullWhen(true)] out Process? process)
        {
            process = null;

            try
            {
                process = Process.GetProcessById(pid);
                return true;
            }
            catch { return false; }
        }
    }
}