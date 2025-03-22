namespace TrebuchetLib.Processes;

public class ConanServerInfos
{
    public int Instance { get; set; }
    public int Port { get; set; }
    public int QueryPort { get; set; }
    public string RConPassword { get; set; } = String.Empty;
    public int RConPort { get; set; }
    public string Title { get; set; } = String.Empty;
}