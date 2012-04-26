using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Forklift
{
    public interface ICommand
    {
        void Run(Args args);
    }

    public abstract class CommandBase : ICommand
    {
        public void Run(Args args)
        {
            var planFile = args.Option("plan", "forklift.plan");
            var extractFile = args.Option("extract", "extract.xml");

            Console.WriteLine("Plan:    {0}", planFile);
            Console.WriteLine("Extract: {0}", extractFile);

            RunCore(args);
        }

        protected abstract void RunCore(Args args);
    }

    public class PushCommand : CommandBase
    {

        protected override void RunCore(Args args)
        {
            Console.WriteLine("Pushing");
        }
    }

    public class PullCommand : CommandBase
    {
        protected override void RunCore(Args args)
        {
            Console.WriteLine("Pulling");
        }
    }

    class Commands
    {
        private static Lazy<ILookup<string, Type>> _commands = new Lazy<ILookup<string, Type>>(() =>
            typeof(Commands).Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(ICommand)))
            .ToLookup(x => x.Name.Replace("Command", "").ToLower()));

        public static ILookup<string, Type> All
        {
            get { return _commands.Value; }
        }

        public static ICommand Find(string name)
        {
            var type = All[name].FirstOrDefault();
            if (type == null)
                return null;

            return (ICommand)Activator.CreateInstance(type);
        }
    }
}
