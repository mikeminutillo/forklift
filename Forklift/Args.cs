using System;
using System.Collections.Generic;
using System.Linq;

namespace Forklift
{
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