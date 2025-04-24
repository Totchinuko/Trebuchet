namespace TrebuchetLib;

public class ClientConnection
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Password { get; set; } = string.Empty;
}