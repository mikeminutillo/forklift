using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using System.Xml.Linq;

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
            var environmentName = args.Arguments.Skip(1).FirstOrDefault();
            var extractions = ExtractionInstructions.Parse(args.Arguments.Skip(2).ToArray());

            if (environmentName == null)
                throw new Exception("You need to specify an environment");

            var environment = Plans.Environment(environmentName);

            using (var context = new DataContext(environment.ConnectionString))
            {
                var metabase = new ContextMetabase(context);
                foreach (var extraction in extractions)
                    Plans.Plan(extraction.ExtractName).UpdateAndCheck(metabase);
                
                //new XElement("Extract", 
                //    extractions.Select(
                //        x => Plans.Plan(x.ExtractName).Run(context, x.IdsToExtract)).Where(x => x != null)
                //    ).Save(ExtractFile);
                // Run the extracts
                // Save the extract file
            }
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
