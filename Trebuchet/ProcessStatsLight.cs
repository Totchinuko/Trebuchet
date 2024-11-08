using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TrebuchetLib;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Trebuchet
{
    public class ProcessStatsLight : INotifyPropertyChanged, IProcessStats
    {
        protected const string CPUFormat = "{0}%";
        protected const string MemoryFormat = "{0}MB (Peak {1}MB)";
        private static DateTime? _previousCpuStartTime = null;
        private static TimeSpan? _previousTotalProcessorTime = null;
        private ProcessDetails? _details;
        private long _peakMemoryConsumption;
        private DispatcherTimer _timer;

        public ProcessStatsLight()
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

        public void StartStats(ProcessDetails details)
        {
            SetDetails(details);

            _timer.Start();
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(PID));
            OnPropertyChanged(nameof(ProcessStatus));
        }

        public void StopStats(ProcessDetails details)
        {
            SetDetails(details);
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
                long memoryConsumption = GetMemeoryUsageForProcess();
                CpuUsage = string.Format(CPUFormat, GetCpuUsageForProcess().ToString("N2"));
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
            if (_details == null) return 0;
            if (!TryGetProcess((int)_details.PID, out Process? process)) return 0;
            var currentCpuStartTime = DateTime.UtcNow;
            var currentCpuUsage = process.TotalProcessorTime;

            // If no start time set then set to now
            if (_previousCpuStartTime == null || _previousTotalProcessorTime == null)
            {
                _previousCpuStartTime = currentCpuStartTime;
                _previousTotalProcessorTime = currentCpuUsage;
            }

            var cpuUsedMs = (currentCpuUsage - _previousTotalProcessorTime.Value).TotalMilliseconds;
            var totalMsPassed = (currentCpuStartTime - _previousCpuStartTime.Value).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            // Set previous times.
            _previousCpuStartTime = currentCpuStartTime;
            _previousTotalProcessorTime = currentCpuUsage;

            return cpuUsageTotal * 1000.0;
        }

        private long GetMemeoryUsageForProcess()
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