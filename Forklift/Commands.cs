using System;
using System.Linq;

namespace Forklift
{
    class Commands
    {
        private static readonly Lazy<ILookup<string, Type>> CommandsLookup = new Lazy<ILookup<string, Type>>(() =>
            typeof(Commands).Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(ICommand)))
            .ToLookup(x => x.Name.Replace("Command", "").ToLower()));

        public static ILookup<string, Type> All
        {
            get { return CommandsLookup.Value; }
        }

        public static ICommand Find(string name)
        {
            var type = All[name.ToLower()].FirstOrDefault();
            if (type == null)
                return null;

            return (ICommand)Activator.CreateInstance(type);
        }
    }
}
