using System;
using System.Linq;

namespace Forklift
{
    public class Plan
    {
        public string Name { get; set; }
        public Segment[] Segments { get; set; }

        public PlanPart GetPlan(UpdateContext updateContext)
        {
            var parts = PlanPart.Parse(Segments);
            foreach (var part in parts)
                part.Update(null, updateContext);

            if (parts.Count() > 1)
            {
                Console.WriteLine("Multi-part plans aren't supported (Yet!)");
            }

            return parts.FirstOrDefault();
        }
    }
}