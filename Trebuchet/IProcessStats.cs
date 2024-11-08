using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrebuchetLib;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

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

        void SetDetails(ProcessDetails details);

        void StartStats(ProcessDetails details);

        void StopStats(ProcessDetails details);
    }
}