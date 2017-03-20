using System;
using System.Diagnostics;
using System.Linq;

namespace Forklift
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var parsedArgs = Args.Parse(args);

                var commandName = parsedArgs.Arguments.FirstOrDefault();

                if (commandName == null)
                {
                    Console.Error.WriteLine("You must supply a command name");
                    ShowUsageAndExit(-1);
                }

                var command = Commands.Find(commandName);

                if (command == null)
                {
                    Console.Error.WriteLine("No command with that name could be found");
                    ShowUsageAndExit(-2);
                }
                else
                {
                    command.Run(parsedArgs);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Something went wrong: {0}", ex);
                ShowUsageAndExit(-3);
            }

            if (Debugger.IsAttached)
                Console.ReadLine();

        }

        private static void ShowUsageAndExit(int p)
        {
            Console.WriteLine("USAGE: forklift.exe <command> <command args> [<options>]");

            if (Debugger.IsAttached)
                Console.ReadLine();
            Environment.Exit(p);
        }
    }
}
