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
        private Lazy<PlanFile> _planFile;

        public PlanFile Plans { get { return _planFile.Value; } }
        public string ExtractFile { get; private set; }

        public void Run(Args args)
        {
            _planFile = new Lazy<PlanFile>(() => PlanFile.Load(args.Option("plan", "forklift.plan")));
            ExtractFile = args.Option("extract", "extract.xml");

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
            Plans.Environment("RAVS");
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
            var type = All[name.ToLower()].FirstOrDefault();
            if (type == null)
                return null;

            return (ICommand)Activator.CreateInstance(type);
        }
    }
}
