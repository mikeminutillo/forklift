using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Forklift
{
    public class HasOnePart : SubPart
    {
        public static IEnumerable<PlanPart> Parse(string text)
        {
            var match = Regex.Match(text, @"([^:\s]+)(?::([^\s]+))?");

            if (match.Success)
            {
                var descriptor = match.Result("$1");
                var key = match.Result("$2");

                var tableName = descriptor.ToTableName();

                foreach (var elementName in descriptor.ToElementNames())
                    yield return new HasOnePart
                                     {
                                         TableName = tableName,
                                         ElementName = elementName,
                                         ForeignKey = key,
                                     };
            }
        }

        protected override void FillIn(PlanPart parent, UpdateContext context)
        {
            base.FillIn(parent, context);

            parent.Ignore(ForeignKey);
        }

    }

}