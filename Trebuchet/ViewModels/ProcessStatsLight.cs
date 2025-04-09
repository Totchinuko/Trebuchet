using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Threading;
using Humanizer;
using Trebuchet.Assets;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public class ProcessStatsLight : BaseViewModel, IProcessStats
    {
        private IConanProcess? _details;
        private long _peakMemoryConsumption;
        private DispatcherTimer _timer;
        private string _playerCount = string.Empty;
        private string _memoryConsumption = string.Empty;
        private string _cpuUsage = string.Empty;
        private string _uptime = string.Empty;

        public ProcessStatsLight()
        {
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => Tick());
            _timer.Stop();
        }

        public string CpuUsage
        {
            get => _cpuUsage;
            private set => SetField(ref _cpuUsage, value);
        }

        public string MemoryConsumption
        {
            get => _memoryConsumption;
            private set => SetField(ref _memoryConsumption, value);
        }

        public int PID => (_details?.PId ?? 0);

        public string PlayerCount
        {
            get => _playerCount;
            private set => SetField(ref _playerCount, value);
        }

        public string ProcessStatus => _details?.State.ToString() ?? string.Empty;

        public bool Running => _details?.State.IsRunning() ?? false;

        public string Uptime
        {
            get => _uptime;
            private set => SetField(ref _uptime, value);
        }

        public void SetDetails(IConanProcess details)
        {
            _details = details;
            OnPropertyChanged(nameof(ProcessStatus));
        }

        public void StartStats(IConanProcess details)
        {
            SetDetails(details);
            _timer.Start();
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(PID));
            OnPropertyChanged(nameof(ProcessStatus));
        }

        public void StopStats()
        {
            _details = null;
            _peakMemoryConsumption = 0;
            _timer.Stop();
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(ProcessStatus));
        }
        
        private async void Tick()
        {
            if (!Running) return;

            long memoryConsumption = _details?.MemoryUsage ?? 0;
            CpuUsage = string.Format(Resources.CpuFormat, (await GetCpuUsageForProcess()).ToString(@"N2"));
            _peakMemoryConsumption = Math.Max(memoryConsumption, _peakMemoryConsumption);
            MemoryConsumption = string.Format(Resources.MemoryFormat, (memoryConsumption / 1024 / 1024), (_peakMemoryConsumption / 1024 / 1024));
            Uptime = _details == null ? string.Empty : (DateTime.UtcNow - _details.StartUtc).Humanize();

            if (_details is IConanServerProcess serverDetails)
                PlayerCount = @$"{serverDetails.Players}/{serverDetails.MaxPlayers}";
        }
        
        private async Task<double> GetCpuUsageForProcess()
        {
            if (_details == null) return 0;
            
            var startTime = DateTime.UtcNow;
            var startUsage = _details.CpuTime;

            await Task.Delay(500);
            
            var endTime = DateTime.UtcNow;
            var endUsage = _details.CpuTime;

            var cpuUsedMs = (endUsage - startUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return cpuUsageTotal * 100.0;
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