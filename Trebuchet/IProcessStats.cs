﻿using TrebuchetLib;
using TrebuchetLib.Processes;

namespace Trebuchet
{
    public interface IProcessStats
    {
        string CpuUsage { get; }
        string MemoryConsumption { get; }
        int PID { get; }
        string PlayerCount { get; }
        string ProcessStatus { get; }
        bool Running { get; }
        string Uptime { get; }

        void SetDetails(IConanProcess details);

        void StartStats(IConanProcess details);

        void StopStats();
    }
}