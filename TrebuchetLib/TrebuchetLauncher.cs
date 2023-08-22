using SteamKit2.Internal;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TrebuchetLib;

namespace Trebuchet
{
    public sealed class TrebuchetLauncher
    {
        public TrebuchetLauncher(bool testlive)
        {
            Config = Config.LoadConfig(Config.GetPath(testlive));
            Steam = new Steam(Config);
            Launcher = new Launcher(Config);
        }

        public Config Config { get; }

        public Launcher Launcher { get; }

        public Steam Steam { get; }
    }
}