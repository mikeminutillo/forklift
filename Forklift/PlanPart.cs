using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Forklift
{
    public abstract class PlanPart
    {
        protected PlanPart()
        {
            Columns = new ColumnMeta[0];
            Children = new PlanPart[0];
        }

        public string TableName { get; protected set; }
        public string ElementName { get; protected set; }
        public TableMeta Table { get; private set; }
        public ColumnMeta[] Columns { get; protected set; }
        public PlanPart[] Children { get; protected set; }

        private readonly ISet<string> _ignoredColumns = new HashSet<string>(); 

        public void Ignore(params string[] columnNames)
        {
            foreach(var columnName in columnNames)
                _ignoredColumns.Add(columnName); // Should we check if they exist at this point?
        }

        public void Ignore(params ColumnMeta[] columns)
        {
            Ignore(columns.Select(x => x.Name).ToArray());
        }

        protected virtual string XmlClause()
        {
            return "TYPE";
        }

        protected virtual string QueryTemplate()
        {
            return @"SELECT 
{0} 
FROM [{2}] [{3}] 
{1}
FOR XML AUTO, {4}";
        }

        protected virtual string WhereClause()
        {
            return "";
        }

        protected IEnumerable<string> ExtractableColumns()
        {
            return from column in Columns 
                   where _ignoredColumns.All(x => column.IsNamed(x) == false)
                   select column.Name;
        }

        public virtual string ToQuery()
        {
            var selectClause = String.Join(",\n",
                                           ExtractableColumns().Select(x => String.Format("[{0}]", x))
                                               .Concat(Children.Select(x => x.ToQuery()))
                );

            var qry = String.Format(QueryTemplate(),

                                    selectClause, WhereClause(), TableName, ElementName, XmlClause()
                );

            return qry;
        }

        protected virtual void FillIn(PlanPart parent, UpdateContext context) { }

        public void Update(PlanPart parent, UpdateContext context)
        {
            //Console.WriteLine("Updating {0}", TableName);
            Table = context.Metabase.Table(TableName);
            Columns = Table.Columns;
            Ignore(Columns.Where(x => x.Insertable() ==false).Select(x => x.Name).ToArray());
            Ignore(Table.PrimaryKey);
            FillIn(parent, context);
            foreach (var child in Children)
                child.Update(this, context);
        }

        protected virtual IEnumerable<XElement> GetElements(XElement parent)
        {
            return parent.Elements(ElementName);
        }

        protected virtual IDictionary<string, object> GetValues(XElement element)
        {
            return element.ToValues();
        }

        protected virtual object PushSelf(IMetabase metabase, IDictionary<string, object> values)
        {
            var key = metabase.Insert(Table.Name, values);
            values[Table.PrimaryKey.Name] = key;
            return key;
        }

        protected virtual void UpdateParentValues(IDictionary<string, object> parentValues, object key)
        {

        }

        protected virtual void UpdateFromParentValues(IDictionary<string, object> myValues, IDictionary<string, object> parentValues)
        {

        }

        public void Process(XElement parent, IMetabase metabase, IDictionary<string, object> parentValues)
        {
            foreach(var element in GetElements(parent))
            {
                var values = GetValues(element);
                UpdateFromParentValues(values, parentValues);

                foreach (var lookup in Children.OfType<LookupPart>())
                    lookup.Process(element, metabase, values);
                
                foreach (var hasOne in Children.OfType<HasOnePart>())
                    hasOne.Process(element, metabase, values);

                var key = PushSelf(metabase, values);

                foreach (var hasMany in Children.OfType<HasManyPart>())
                    hasMany.Process(element, metabase, values);

                UpdateParentValues(parentValues, key);
            }
        }


        public static PlanPart[] Parse(params Segment[] segments)
        {
            return segments.Select(x => Parse(x, default(PlanPart))).SelectMany(x => x).ToArray();
        }

        private static IEnumerable<PlanPart> Parse(Segment segment, PlanPart parent)
        {
            var parts = new List<PlanPart>();

            if (parent == default(PlanPart))
            {
                parts.AddRange(RootPlanPart.Parse(segment.Text));
            }
            else
            {
                var text = segment.Text.Substring(1);
                var prefix = segment.Text[0];

                switch (prefix)
                {
                    case '@':
                        parts.AddRange(LookupPart.Parse(text));
                        break;
                    case '<':
                        parts.AddRange(HasManyPart.Parse(text));
                        break;
                    case '>':
                        parts.AddRange(HasOnePart.Parse(text));
                        break;
                    default:
                        Console.Error.WriteLine("Invalid: " + segment.Text);
                        break;
                }
            }

            foreach (var part in parts)
            {
                part.Children = segment.SubSegments.SelectMany(x => Parse(x, part)).ToArray();
            }

            return parts;
        }
    }
}