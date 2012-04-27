using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Forklift
{
    public class HasManyPart : SubPart
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
                    yield return new HasManyPart
                    {
                        TableName = tableName,
                        ElementName = elementName,
                        ForeignKey = key,
                    };
            }

        }

        protected override void FillIn(PlanPart parent, UpdateContext context)
        {
            if (String.IsNullOrWhiteSpace(ForeignKey)) ForeignKey = parent.Table.Name + parent.Table.PrimaryKey.Name;
            Ignore(ForeignKey);
            base.FillIn(parent, context);
        }

        protected override string WhereClause()
        {
            return String.Format("WHERE [{0}].[{1}] = [{2}].[{3}]",
                                 Table.Name, ForeignKey,
                                 ParentTable.Name, ParentTable.PrimaryKey.Name
                );
        }

        protected override void UpdateParentValues(IDictionary<string, object> parentValues, object key)
        {
            // HasMany does not update the parent values
        }
        
        protected override void UpdateFromParentValues(IDictionary<string, object> myValues, IDictionary<string, object> parentValues)
        {
            base.UpdateFromParentValues(myValues, parentValues);

            var parentValue = parentValues[ParentTable.PrimaryKey.Name];

            myValues[ForeignKey] = parentValue;

        }
    }
}