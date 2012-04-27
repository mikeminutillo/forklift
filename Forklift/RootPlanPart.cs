using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Forklift
{
    public class RootPlanPart : PlanPart
    {
        public string Discriminator { get; private set; }
        public string ExtractName { get; private set; }
        public string[] IdsToExtract { get; private set; }
        public ColumnMeta DiscriminatorColumn { get; private set; }

        public RootPlanPart()
        {
            IdsToExtract = new string[0];
        }

        public static IEnumerable<PlanPart> Parse(string text)
        {
            var match = Regex.Match(text, @"([^:]+)(?::(.+))?");

            if (match.Success)
            {
                var descriptor = match.Result("$1");
                var discriminator = match.Result("$2");
                var tableName = descriptor.ToTableName();

                foreach (var elementName in descriptor.ToElementNames())
                    yield return new RootPlanPart { TableName = tableName, ElementName = elementName, Discriminator = discriminator };
            }
        }

        protected override void FillIn(PlanPart parent, UpdateContext context)
        {
            base.FillIn(parent, context);
            ExtractName = context.Instructions.ExtractName;
            IdsToExtract = context.Instructions.IdsToExtract;
            DiscriminatorColumn = Table.Columns.FirstOrDefault(x => x.IsNamed(Discriminator))
                ?? Table.PrimaryKey;
        }

        protected override string WhereClause()
        {
            if(DiscriminatorColumn == null || IdsToExtract.Any() == false)
                return base.WhereClause();

            return String.Format("WHERE {0} IN ({1})", DiscriminatorColumn.Name, 
                String.Join(", ", IdsToExtract.Select(x => DiscriminatorColumn.Stringify(x)))
            );
        }

        protected override string XmlClause()
        {
            return String.Format("ROOT('{0}')", ExtractName);
        }
    }
}