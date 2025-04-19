using System;
using System.IO;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels;

public class SteamProgressRestore(Steam steam) : IDisposable
{
    public void Dispose()
    {
        steam.RestoreProgress();
    }
}