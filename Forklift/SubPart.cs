using System;
using System.Collections.Generic;
using System.Linq;

namespace Forklift
{
    public abstract class SubPart : PlanPart
    {
        public TableMeta ParentTable { get; protected set; }
        public string ForeignKey { get; protected set; }
        public string ParentElementName { get; set; }

        protected override void FillIn(PlanPart parent, UpdateContext context)
        {
            ParentTable = parent.Table;
            ParentElementName = parent.ElementName;

            if (String.IsNullOrEmpty(ForeignKey)) ForeignKey = ElementName + Table.PrimaryKey.Name;
            
        }

        protected override string WhereClause()
        {
            return String.Format("WHERE [{0}].[{1}] = [{2}].[{3}]",
                                 ElementName, Table.PrimaryKey.Name,
                                 ParentTable.Name, ForeignKey
                );
        }

        protected override string QueryTemplate()
        {
            return "(" + base.QueryTemplate() + ")";
        }

        protected override void UpdateParentValues(IDictionary<string, object> parentValues, object key)
        {
            base.UpdateParentValues(parentValues, key);

            var column = ParentTable.Columns.SingleOrDefault(x => x.IsNamed(ForeignKey));

            if(column == null)
            {
                Console.WriteLine("There was no column named {0} on the {1} table", ForeignKey, ParentTable.Name);
                return;
            }

            //Console.WriteLine("Updating {0}.{1} to have value: {2}", ParentTable.Name, ForeignKey, column.Stringify(key));

            //Console.WriteLine("Adding: {0} = {1}", ForeignKey, column.Stringify(key));
            if (key != null)
                parentValues[column.Name] = column.Stringify(key);
        }
    }
}