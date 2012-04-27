using System;
using System.Data.Linq;
using System.Linq;
using System.Transactions;
using System.Xml.Linq;

namespace Forklift
{
    public class PushCommand : CommandBase
    {
        protected override void RunCore(Args args)
        {
            var environmentName = args.Arguments.Skip(1).FirstOrDefault();
            var extractions = ExtractionInstructions.Parse(args.Arguments.Skip(2).ToArray());

            if (environmentName == null)
                throw new Exception("You need to specify an environment");

            var environment = Plans.Environment(environmentName);

            using (var context = new DataContext(environment.ConnectionString))
            using (var scope = new TransactionScope())
            {
                var metabase = new ContextMetabase(context);

                foreach (var extraction in extractions)
                    extraction.Update(metabase, Plans);

                var extract = XElement.Load(ExtractFile);

                foreach (var extraction in extractions)
                    extraction.Insert(metabase, extract);
                
                scope.Complete();
            }
        }
    }
}