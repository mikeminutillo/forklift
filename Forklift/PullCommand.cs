using System;
using System.Data.Linq;
using System.Linq;
using System.Xml.Linq;

namespace Forklift
{
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
                    extraction.Update(metabase, Plans);
                    

                new XElement("Extract",
                             extractions.Select(x => x.Run(metabase))
                    ).Save(ExtractFile);
            }
        }
    }
}