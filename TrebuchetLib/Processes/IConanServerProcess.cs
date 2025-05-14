namespace TrebuchetLib.Processes;

public interface IConanServerProcess : IConanProcess
{
    ConanServerInfos Infos { get; }
    
    public int MaxPlayers { get; }

    public int Players { get; }

    public bool Online { get; }
    
    public bool RequestRestart { get; }
    
    public IRcon? RCon { get; }
    
    public bool KillZombies { get; set; }
    
    public int ZombieCheckSeconds { get; set; }

    Task StopAsync();

    Task RestartAsync();
}