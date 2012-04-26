using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Forklift
{
    class ExtractionInstructions
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

        public XElement Run(Func<string, string> runner)
        {
            Func<PlanPart, XElement> converter = p => 
                {
                    var query = p.ToQuery();
                    var results = runner(query); // TODO: Add the query clause
                    return String.IsNullOrWhiteSpace(results) ? null : XElement.Parse(results);
                };

            return converter(Plan);
        }
    }
}
