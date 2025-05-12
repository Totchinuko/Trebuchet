using System.Text.RegularExpressions;
using Discord.Webhook;
using TrebuchetLib.Services;

namespace TrebuchetLib;

public partial class DiscordWebHooks(ServerProfile profile) : INotifier
{
    public async Task Notify(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (string.IsNullOrEmpty(profile.DiscordWebHookNotifications)) return;
        
        var matches = DiscordWebHooksRegex().Match(profile.DiscordWebHookNotifications);
        if (!matches.Success) return;
        
        using var discord = new DiscordWebhookClient(profile.DiscordWebHookNotifications);
        await discord.SendMessageAsync(message);
    }

    [GeneratedRegex("https:\\/\\/discord\\.com\\/api\\/webhooks\\/([0-9]+)\\/([\\w]+)")]
    public static partial Regex DiscordWebHooksRegex();
}