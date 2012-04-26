using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

                command.Run(parsedArgs);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Something went wrong: {0}", ex);
                ShowUsageAndExit(-3);
            }

        }

        private static void ShowUsageAndExit(int p)
        {
            Console.WriteLine("USAGE: forklift.exe <command> <command args> [<options>]");
            Environment.Exit(p);
        }
    }

    public class Args
    {
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        private readonly IList<string> _arguments = new List<string>();

        public string[] Arguments
        {
            get
            {
                return _arguments.ToArray();
            }
        }

        public string Option(string name, string @default = null)
        {
            if (_options.ContainsKey(name))
                return _options[name];
            return @default;
        }

        private Args()
        {
        }

        public static Args Parse(string[] args)
        {
            var parsedArgs = new Args();

            var counter = 0;
            while (counter < args.Length)
            {
                var arg = args[counter];
                if (arg.StartsWith("-"))
                {
                    var name = arg.Substring(1);
                    counter += 1;
                    if (counter >= args.Length)
                        throw new Exception("No value provided for option " + name);
                    var value = args[counter];
                    parsedArgs._options.Add(name, value);
                }
                else
                {
                    parsedArgs._arguments.Add(arg);
                }
                counter += 1;
            }


            return parsedArgs;
        }
    }
}
