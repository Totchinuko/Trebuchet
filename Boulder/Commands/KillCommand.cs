using System.CommandLine;
using tot_lib.CommandLine;

namespace Boulder.Commands;

public class KillCommand : ICommand<KillCommand>
{
    public static readonly Command Command = CommandBuilder
        .Create<KillCommand>("kill", "Kill a server/client process")
        .SubCommands.Add(KillServerCommand.Command)
        .SubCommands.Add(KillClientCommand.Command)
        .BuildCommand();
}