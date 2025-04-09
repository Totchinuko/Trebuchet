using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using Humanizer;
using ReactiveUI;
using Trebuchet.Assets;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public class ProcessStatsLight : ReactiveObject, IProcessStats
    {
        public ProcessStatsLight()
        {
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => Tick());
            _timer.Stop();
            _details = ConanProcess.Empty;

            this.WhenAnyValue(x => x.Details)
                .Subscribe((d) =>
                {
                    if (d.State.IsRunning())
                    {
                        State = d.State;
                        _timer.Start();
                    }
                    else
                    {
                        State = ProcessState.STOPPED;
                        _peakMemoryConsumption = 0;
                        _timer.Stop();
                    }
                });

            _pid = this.WhenAnyValue(x => x.Details)
                .OfType<IConanProcess>()
                .Select(x => x.PId)
                .ToProperty(this, x => x.PID);
            _processStatus = this.WhenAnyValue(x => x.State)
                .Select(x => TranslateState(x))
                .ToProperty(this, x => x.ProcessStatus);
            _running = this.WhenAnyValue(x => x.State)
                .Select(x => x.IsRunning())
                .ToProperty(this, x => x.Running);
        }
        
        private IConanProcess _details;
        private long _peakMemoryConsumption;
        private DispatcherTimer _timer;
        private string _playerCount = string.Empty;
        private string _memoryConsumption = string.Empty;
        private string _cpuUsage = string.Empty;
        private string _uptime = string.Empty;
        private ProcessState _state;
        private ObservableAsPropertyHelper<int> _pid;
        private ObservableAsPropertyHelper<string> _processStatus;
        private ObservableAsPropertyHelper<bool> _running;

        public string CpuUsage
        {
            get => _cpuUsage;
            private set => this.RaiseAndSetIfChanged(ref _cpuUsage, value);
        }

        public string MemoryConsumption
        {
            get => _memoryConsumption;
            private set => this.RaiseAndSetIfChanged(ref _memoryConsumption, value);
        }

        public ProcessState State
        {
            get => _state;
            set => this.RaiseAndSetIfChanged(ref _state, value);
        }

        public IConanProcess Details
        {
            get => _details;
            set => this.RaiseAndSetIfChanged(ref _details, value);
        }

        public int PID => _pid.Value;

        public string PlayerCount
        {
            get => _playerCount;
            private set => this.RaiseAndSetIfChanged(ref _playerCount, value);
        }

        public string ProcessStatus => _processStatus.Value;

        public bool Running => _running.Value;

        public string Uptime
        {
            get => _uptime;
            private set => this.RaiseAndSetIfChanged(ref _uptime, value);
        }

        private string TranslateState(ProcessState state)
        {
            switch (state)
            {
                case ProcessState.NEW:
                    return String.Empty;
                case ProcessState.STOPPED:
                    return String.Empty;
                case ProcessState.FAILED:
                    return String.Empty;
                case ProcessState.STOPPING:
                    return String.Empty;
                case ProcessState.RUNNING:
                    return String.Empty;
                case ProcessState.ONLINE:
                    return String.Empty;
                case ProcessState.CRASHED:
                    return String.Empty;
                default:
                    return String.Empty;
            }
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