using TrebuchetLib.Services;

namespace TrebuchetLib;

public static class UserDefinedNotifications
{
    public static string GetCrashNotification(this AppSetup setup, string serverName)
    {
        var template = setup.Config.NotificationServerCrash;
        return template.Replace("{serverName}", serverName);
    }

    public static string GetOnlineNotification(this AppSetup setup, string serverName)
    {
        var template = setup.Config.NotificationServerOnline;
        return template.Replace("{serverName}", serverName);
    }
}