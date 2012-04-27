using System;

namespace Forklift
{
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
}