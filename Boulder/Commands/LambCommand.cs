using System.CommandLine;
using tot_lib;
using tot_lib.CommandLine;

namespace Boulder.Commands;

public class LambCommand : ICommand<LambCommand>
{
    public static readonly Command Command = CommandBuilder
        .Create<LambCommand>("lamb", "Start a conan process and sacrifice itself")
        .SubCommands.Add(LambClientCommand.Command)
        .SubCommands.Add(LambServerCommand.Command)
        .BuildCommand();
}