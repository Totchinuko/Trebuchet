using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
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
            var timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => Tick());
            timer.Stop();
            _details = ConanProcess.Empty;

            this.WhenAnyValue(x => x.Details)
                .Subscribe((d) =>
                {
                    if (d.State.IsRunning())
                    {
                        State = d.State;
                        timer.Start();
                    }
                    else
                    {
                        State = ProcessState.STOPPED;
                        _peakMemoryConsumption = 0;
                        timer.Stop();
                    }
                });

            _pid = this.WhenAnyValue(x => x.Details)
                .OfType<IConanProcess>()
                .Select(x => x.PId)
                .ToProperty(this, x => x.PID);
            _processStatus = this.WhenAnyValue(x => x.State)
                .Select(TranslateState)
                .ToProperty(this, x => x.ProcessStatus);
            _running = this.WhenAnyValue(x => x.State)
                .Select(x => x.IsRunning())
                .ToProperty(this, x => x.Running);
        }
        
        private readonly ObservableAsPropertyHelper<int> _pid;
        private readonly ObservableAsPropertyHelper<string> _processStatus;
        private readonly ObservableAsPropertyHelper<bool> _running;
        private IConanProcess _details;
        private long _peakMemoryConsumption;
        private string _playerCount = string.Empty;
        private string _memoryPeakConsumption = string.Empty;
        private string _memoryConsumption = string.Empty;
        private string _cpuUsage = string.Empty;
        private string _uptime = string.Empty;
        private ProcessState _state;

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

        public string MemoryPeakConsumption
        {
            get => _memoryPeakConsumption;
            set => this.RaiseAndSetIfChanged(ref _memoryPeakConsumption, value);
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
                    return Resources.StatusNew;
                case ProcessState.STOPPED:
                    return Resources.StatusStopped;
                case ProcessState.FAILED:
                    return Resources.StatusFailed;
                case ProcessState.STOPPING:
                    return Resources.StatusStopping;
                case ProcessState.RUNNING:
                    return Resources.StatusRunning;
                case ProcessState.ONLINE:
                    return Resources.StatusOnline;
                case ProcessState.CRASHED:
                    return Resources.StatusCrashed;
                case ProcessState.FROZEN:
                    return Resources.StatusFrozen;
                default:
                    return String.Empty;
            }
        }

        private async void Tick()
        {
            try
            {
                await TickAsync();
            }
            catch (OperationCanceledException){}
            catch (Exception ex)
            {
                await CrashHandler.Handle(ex);
            }
        }

        private async Task TickAsync()
        {
            if (!Running) return;

            State = _details.State;
            long memoryConsumption = _details.MemoryUsage;
            CpuUsage = string.Format(Resources.CpuFormat, (await GetCpuUsageForProcess()).ToString(@"N2"));
            _peakMemoryConsumption = Math.Max(memoryConsumption, _peakMemoryConsumption);
            MemoryConsumption = memoryConsumption.Bytes().Humanize();
            MemoryPeakConsumption = _peakMemoryConsumption.Bytes().Humanize();
            Uptime = (DateTime.UtcNow - _details.StartUtc).Humanize();

            if (_details is IConanServerProcess serverDetails)
                PlayerCount = @$"{serverDetails.Players}/{serverDetails.MaxPlayers}";
        }
        
        private async Task<double> GetCpuUsageForProcess()
        {
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
    }
}