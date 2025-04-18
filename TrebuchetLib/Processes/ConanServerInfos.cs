namespace TrebuchetLib.Processes;

public class ConanServerInfos
{
    public ConanServerInfos()
    {
    }

    public ConanServerInfos(ServerProfile profile, int instance)
    {
        Instance = instance;
        Port = profile.GameClientPort;
        QueryPort = profile.SourceQueryPort;
        RConPassword = profile.RConPassword;
        RConPort = profile.RConPort;
        Title = profile.ServerName;
    }
    
    public int Instance { get; set; }
    public int Port { get; set; }
    public int QueryPort { get; set; }
    public string RConPassword { get; set; } = string.Empty;
    public int RConPort { get; set; }
    public string Title { get; set; } = string.Empty;
}