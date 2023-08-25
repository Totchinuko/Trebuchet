using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trebuchet;

namespace TrebuchetLib
{
    public class ProcessDetails
    {
        public ProcessDetails(ProcessDetails details, ProcessData data, ProcessState state)
        {
            Arguments = data.args;
            Filename = data.filename;
            PID = (uint)data.pid;
            ProcessName = data.processName;
            State = state;
            WorkingDirectory = Path.GetDirectoryName(Filename) ?? string.Empty;
            StartUtc = data.start;
            Modlist = details.Modlist;
            Profile = details.Profile;
        }

        public ProcessDetails(string profile, string modlist)
        {
            Profile = profile;
            Modlist = modlist;
            Arguments = string.Empty;
            Filename = string.Empty;
            PID = 0;
            StartUtc = DateTime.MinValue;
            ProcessName = string.Empty;
            State = ProcessState.NEW;
            WorkingDirectory = string.Empty;
        }

        public ProcessDetails(string profile, string modlist, ProcessData data, ProcessState state)
        {
            Profile = profile;
            Modlist = modlist;
            State = state;
            Arguments = data.args;
            Filename = data.filename;
            PID = (uint)data.pid;
            ProcessName = data.processName;
            StartUtc = data.start;
            WorkingDirectory = Path.GetDirectoryName(Filename) ?? string.Empty;
        }

        public ProcessDetails(ProcessDetails details, ProcessState state)
        {
            Arguments = details.Arguments;
            Filename = details.Filename;
            PID = details.PID;
            ProcessName = details.ProcessName;
            State = state;
            WorkingDirectory = details.WorkingDirectory;
            Profile = details.Profile;
            Modlist = details.Modlist;
            StartUtc = details.StartUtc;
        }

        public ProcessDetails()
        {
            Arguments = string.Empty;
            Filename = string.Empty;
            PID = 0;
            ProcessName = string.Empty;
            State = ProcessState.NEW;
            WorkingDirectory = string.Empty;
            Profile = string.Empty;
            Modlist = string.Empty;
            StartUtc = DateTime.MinValue;
        }

        public string Arguments { get; }

        public string Filename { get; }

        public string Modlist { get; }

        public uint PID { get; }

        public string ProcessName { get; }

        public string Profile { get; }

        public DateTime StartUtc { get; }

        public ProcessState State { get; }

        public string WorkingDirectory { get; }
    }

    public class ProcessDetailsEventArgs
    {
        public ProcessDetailsEventArgs(ProcessDetails oldDetails, ProcessDetails newDetails)
        {
            OldDetails = oldDetails;
            NewDetails = newDetails;
        }

        public ProcessDetails NewDetails { get; }

        public ProcessDetails OldDetails { get; }
    }
}