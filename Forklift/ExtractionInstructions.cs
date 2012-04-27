using System;
using System.Linq;
using System.Xml.Linq;

namespace Forklift
{
    public class ExtractionInstructions
    {
        public string ExtractName { get; set; }
        public string[] IdsToExtract { get; set; }
        public PlanPart Plan { get; set; }

        public static ExtractionInstructions[] Parse(string[] args)
        {
            return (from e in
                        (
                            from a in args
                            let split = a.Split(':')
                            select new
                            {
                                extractName = split.First(),
                                ids = split.Skip(1).Select(x => x.Split(',')).FirstOrDefault()
                            }
                        )
                    group e by e.extractName into g
                    select new ExtractionInstructions
                    {
                        ExtractName = g.Key,
                        IdsToExtract = g.Where(x => x.ids != null).SelectMany(x => x.ids).ToArray()
                    }).ToArray();
        }

        public XElement Run(IMetabase metabase)
        {
            var query = Plan.ToQuery(); 
            Console.WriteLine(query); // For debugging purposes
            return metabase.Query<string>(query).AsXElement();
        }

        public void Update(IMetabase metabase, PlanFile plans)
        {
            var updateContext = new UpdateContext {Instructions = this, Metabase = metabase};
            var plan = plans.Plan(ExtractName);
            ExtractName = plan.Name;
            Plan = plan.GetPlan(updateContext);
        }

        public void Insert(IMetabase metabase, XElement extract)
        {
            var extractElement = extract.Element(ExtractName);
            if (extractElement != null)
                Plan.Process(extractElement, metabase, null);
        }
    }
}
