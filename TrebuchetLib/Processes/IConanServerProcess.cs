namespace TrebuchetLib.Processes;

public interface IConanServerProcess : IConanProcess
{
    public int Instance { get; }

    public int MaxPlayers { get; }

    public int Players { get; }

    public int Port { get; }

    public int QueryPort { get; }

    public string RConPassword { get; }

    public int RConPort { get; }

    public string Title { get; }
    
    public bool Online { get; }

    Task StopAsync();
}