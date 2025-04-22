using System.Reflection;
using TrebuchetLib.Processes;
using TrebuchetLib.Services;
using Yuu.Ini;

namespace TrebuchetLib.YuuIni;

public class YuuIniServerFiles(AppSetup setup)
{
    public async Task WriteIni(ServerProfile profile, int instance)
    {
        Dictionary<string, IniDocument> documents = new Dictionary<string, IniDocument>();
        // Modify the default SectionName parse because funcom sometime does an oupsi and generate sections with an empty name
        var iniParserConfiguration = new IniParserConfiguration();
        iniParserConfiguration.SectionNameRegex = "\\s*[^\\[\\]]*\\s*";

        foreach (var method in GetIniMethods(this))
        {
            IniSettingAttribute attr = method.GetCustomAttribute<IniSettingAttribute>() ?? throw new Exception($"{method.Name} does not have IniSettingAttribute.");
            if (!documents.TryGetValue(attr.Path, out IniDocument? document))
            {
                var path = Path.Combine(setup.GetInstancePath(instance), attr.Path);
                var content = await Tools.GetFileContent(path);
                document = IniParser.Parse(content, iniParserConfiguration);
                documents.Add(attr.Path, document);
            }
            method.Invoke(this, [profile, document]);
        }

        foreach (var document in documents)
        {
            document.Value.MergeDuplicateSections();
            var path = Path.Combine(setup.GetInstancePath(instance), document.Key);
            await Tools.SetFileContent(path, document.Value.ToString()).ConfigureAwait(false);
        }
    }

    public async Task<ConanServerInfos> GetInfosFromIni(int instance)
    {
        var infos = new ConanServerInfos();
        infos.Instance = instance;

        var instancePath = setup.GetInstancePath(instance);
        string initPath = Path.Combine(instancePath, string.Format(Constants.FileIniServer, "Engine"));
        IniDocument document = IniParser.Parse(await Tools.GetFileContent(initPath));

        IniSection section = document.GetSection("OnlineSubsystem");
        infos.Title = section.GetValue("ServerName", "Conan Exile Dedicated Server");

        section = document.GetSection("URL");
        infos.Port = section.GetValue("Port", 7777);

        section = document.GetSection("OnlineSubsystemSteam");
        infos.QueryPort = section.GetValue("GameServerQueryPort", 27015);

        document = IniParser.Parse(await Tools.GetFileContent(Path.Combine(instancePath, string.Format(Constants.FileIniServer, "Game"))));
        section = document.GetSection("RconPlugin");
        infos.RConPassword = section.GetValue("RconPassword", string.Empty);
        infos.RConPort = section.GetValue("RconEnabled", false) ? section.GetValue("RconPort", 25575) : 0;
        return infos;
    }
    
    [IniSetting(Constants.FileIniServer, "Engine")]
    public void ApplyAiSpawn(ServerProfile profile, IniDocument document)
    {
        document.GetSection("/Script/ConanSandbox.SystemSettings").SetParameter("dw.EnableAISpawning", profile.NoAISpawn ? "0" : "1");
        document.GetSection("/Script/ConanSandbox.SystemSettings").SetParameter("dw.EnableInitialAISpawningPass", profile.NoAISpawn ? "0" : "1");
    }

    [IniSetting(Constants.FileIniServer, "Engine")]
    public void ApplyEngineSettings(ServerProfile profile, IniDocument document)
    {
        IniSection section = document.GetSection("OnlineSubsystem");
        section.SetParameter("ServerName", profile.ServerName);
        section.SetParameter("ServerPassword", profile.ServerPassword);

        section = document.GetSection("URL");
        section.SetParameter("Port", profile.GameClientPort.ToString());

        section = document.GetSection("OnlineSubsystemSteam");
        section.SetParameter("GameServerQueryPort", profile.SourceQueryPort.ToString());

        section = document.GetSection("/Script/OnlineSubsystemUtils.IpNetDriver");
        section.SetParameter("NetServerMaxTickRate", profile.MaximumTickRate.ToString());

        section = document.GetSection("Core.Log");
        section.GetParameters().ForEach(section.Remove);

        if (profile.LogFilters.Count > 0)
            foreach (string filter in profile.LogFilters)
            {
                string[] content = filter.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                section.AddParameter(content[0], content[1]);
            }
        else
            document.Remove(section);
    }

    [IniSetting(Constants.FileIniServer, "Game")]
    public void ApplyGameSettings(ServerProfile profile, IniDocument document)
    {
        IniSection section = document.GetSection("/Script/Engine.GameSession");
        section.SetParameter("MaxPlayers", profile.MaxPlayers.ToString());

        section = document.GetSection("RconPlugin");
        section.SetParameter("RconEnabled", profile.EnableRCon.ToString());
        section.SetParameter("RconPort", profile.RConPort.ToString());
        section.SetParameter("RconPassword", profile.RConPassword);
        section.SetParameter("RconMaxKarma", profile.RConMaxKarma.ToString());
    }

    [IniSetting(Constants.FileIniServer, "ServerSettings")]
    public void ApplyServerSettings(ServerProfile profile, IniDocument document)
    {
        IniSection section = document.GetSection("ServerSettings");
        section.SetParameter("serverRegion", profile.ServerRegion.ToString());
        section.SetParameter("AdminPassword", profile.AdminPassword);
        section.SetParameter("IsBattlEyeEnabled", profile.EnableBattleEye.ToString());
        section.SetParameter("IsVACEnabled", profile.EnableVAC.ToString());
    }

    [IniSetting(Constants.FileIniDefault, "Engine")]
    public void ApplySudoSettings(ServerProfile profile, IniDocument document)
    {
        IniSection section = document.GetSection("/Game/Mods/ModAdmin/Auth/EA_MC_Auth.EA_MC_Auth_C");
        section.GetParameters("+SuperAdminSteamIDs").ForEach(section.Remove);

        if (profile.SudoSuperAdmins.Count != 0)
            foreach (string id in profile.SudoSuperAdmins)
                section.InsertParameter(0, "+SuperAdminSteamIDs", id);
        else
            document.Remove(section);

        section = document.GetSection("/Game/Mods/TotAdmin/Tot_AC_Buildable_RPC.Tot_AC_Buildable_RPC_C");
        section.SetParameter("DisableHighPrecisionMoveTool", profile.DisableHighPrecisionMoveTool ? "True" : "False");
    }
    
    private IEnumerable<MethodInfo> GetIniMethods(object target)
    {
        return target.GetType().GetMethods()
            .Where(meth => meth.GetCustomAttributes(typeof(IniSettingAttribute), true).Any())
            .Where(meth =>
                meth.GetParameters().Length == 2 && 
                meth.GetParameters()[0].ParameterType == typeof(ServerProfile) &&
                meth.GetParameters()[1].ParameterType == typeof(IniDocument)
            );
    }
}