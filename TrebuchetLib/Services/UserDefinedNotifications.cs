namespace TrebuchetLib.Services;

public class UserDefinedNotifications(AppSetup setup)
{
    public string GetCrashNotification(string serverName)
    {
        var template = setup.Config.NotificationServerCrash;
        return template.Replace("{serverName}", serverName);
    }

    public string GetOnlineNotification(string serverName)
    {
        var template = setup.Config.NotificationServerOnline;
        return template.Replace("{serverName}", serverName);
    }
}