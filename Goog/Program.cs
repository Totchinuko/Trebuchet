using CommandLine;
using System.Reflection;
using System.Windows.Input;

namespace Goog
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Type[] commands = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(ICommand))).ToArray();

            Parser.Default.ParseArguments(args, commands)
                .WithParsed(Run)
                .WithNotParsed(HandleErrors);
        }

        private static void HandleErrors(IEnumerable<Error> enumerable)
        {
#if DEBUG
            foreach (Error error in enumerable)
            {
                string err = error.ToString() ?? "";
                if (!string.IsNullOrEmpty(err))
                    Tools.WriteColoredLine(err, ConsoleColor.Red);
            }
#endif
            Environment.Exit(1);
        }

        private static void Run(object obj)
        {
            if (obj is not ICommand command)
                throw new Exception("Command is not implemented correctly");
            try
            {
                command.Execute();
            }
            catch (Exception ex)
            {
                Tools.WriteColoredLine($"Error: {ex.Message}", ConsoleColor.Red);
#if DEBUG
                Tools.WriteColoredLine($"{ex.TargetSite}", ConsoleColor.Red);
                Tools.WriteColoredLine($"{ex.StackTrace}", ConsoleColor.Red);
#endif
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }
    }
}