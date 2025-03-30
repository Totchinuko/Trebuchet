using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using TrebuchetLib;
using TrebuchetLib.Processes;

namespace Trebuchet.ViewModels
{
    public class ProcessStatsLight : INotifyPropertyChanged, IProcessStats
    {
        private readonly UIConfig _uiConfig;
        protected const string CpuFormat = "{0}%";
        protected const string MemoryFormat = "{0}MB (Peak {1}MB)";
        private IConanProcess? _details;
        private long _peakMemoryConsumption;
        private Process? _process;

        public ProcessStatsLight(UIConfig uiConfig)
        {
            _uiConfig = uiConfig;
            CpuUsage = string.Empty;
            MemoryConsumption = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CpuUsage { get; private set; }
        public string MemoryConsumption { get; private set; }

        public int PID => (_details?.PId ?? 0);

        public string PlayerCount { get; private set; } = string.Empty;

        public string ProcessStatus => _details?.State.ToString() ?? string.Empty;

        public bool Running => _details?.State.IsRunning() ?? false;

        public string Uptime => _details == null ? string.Empty : (DateTime.UtcNow - _details.StartUtc).ToString("d'd.'h'h:'m'm:'s's'");

        public void SetDetails(IConanProcess details)
        {
            _details = details;
            OnPropertyChanged(nameof(ProcessStatus));
        }

        public void StartStats(IConanProcess details)
        {
            if (!TryGetProcess(details.PId, out Process? process)) return;
            _process = process;

            SetDetails(details);
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(PID));
            OnPropertyChanged(nameof(ProcessStatus));
        }

        public void StopStats()
        {
            _details = null;
            _process = null;
            _peakMemoryConsumption = 0;
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(ProcessStatus));
        }
        
        public async void Tick()
        {
            if (!Running) return;

            if (_uiConfig.DisplayProcessPerformance)
            {
                long memoryConsumption = GetMemoryUsageForProcess();
                CpuUsage = string.Format(CpuFormat, (await GetCpuUsageForProcess()).ToString("N2"));
                _peakMemoryConsumption = Math.Max(memoryConsumption, _peakMemoryConsumption);
                MemoryConsumption = string.Format(MemoryFormat, (memoryConsumption / 1024 / 1024), (_peakMemoryConsumption / 1024 / 1024));
            }

            if (_details is IConanServerProcess serverDetails)
            {
                PlayerCount = $"{serverDetails.Players}/{serverDetails.MaxPlayers}";
                OnPropertyChanged(nameof(PlayerCount));
            }

            OnPropertyChanged(nameof(MemoryConsumption));
            OnPropertyChanged(nameof(CpuUsage));
            OnPropertyChanged(nameof(Uptime));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task<double> GetCpuUsageForProcess()
        {
            if (_process == null) return 0;
            
            var startTime = DateTime.UtcNow;
            var startUsage = _process.TotalProcessorTime;

            await Task.Delay(500);
            
            var endTime = DateTime.UtcNow;
            var endUsage = _process.TotalProcessorTime;

            var cpuUsedMs = (endUsage - startUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return cpuUsageTotal * 100.0;
        }

        private long GetMemoryUsageForProcess()
        {
            if (_details == null) return 0;
            if (!TryGetProcess(_details.PId, out Process? process)) return 0;
            var workingSet64 = process.WorkingSet64;

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