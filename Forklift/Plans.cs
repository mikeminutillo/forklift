using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Forklift
{
    public class Env
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
    }

    public class Plan
    {
        public string Name { get; set; }
        public Segment[] Segments { get; set; }

        public PlanPart GetPlan(IMetabase metabase)
        {
            var parts = PlanPart.Parse(Segments);
            var updateContext = new UpdateContext { Plan = this, Metabase = metabase };
            foreach (var part in parts)
                part.Update(null, updateContext);

            if (parts.Count() > 1)
            {
                Console.WriteLine("Multi-part plans aren't supported (Yet!)");
            }

            return parts.FirstOrDefault();
        }
    }

    public class PlanFile
    {
        private ILookup<string, Env> _environments;
        private ILookup<string, Plan> _plans;

        private PlanFile(IEnumerable<Env> environments, IEnumerable<Plan> plans)
	    {
            _environments = environments.ToLookup(x => x.Name, StringComparer.CurrentCultureIgnoreCase);
            _plans = plans.ToLookup(x => x.Name, StringComparer.CurrentCultureIgnoreCase);
	    }

        public Env Environment(string name)
        {
            var env =  _environments[name].FirstOrDefault();

            if(env != null)
                return env;

            Console.Error.WriteLine("No environment named " + name + " so assuming local sql express database with that name");

            return new Env
            {
                Name = name,
                ConnectionString = @"server=.\SQLEXPRESS;integrated security=sspi; database=" + name
            };
        }

        public Plan Plan(string name)
        {
            var plan = _plans[name].FirstOrDefault();

            if(plan != null)
                return plan;

            Console.Error.WriteLine("No plan found named " + name + " so assuming a table with that name");

            return new Plan
            {
                Name = name,
                Segments = Segment.Parse(name).ToArray()
            };
        }

        public static PlanFile Load(string planFile)
        {
            if (File.Exists(planFile) == false)
            {
                Console.Error.WriteLine("Plan file cannot be found. Specify with -plan <planfile>. Defaults to forklift.plan");
                return new PlanFile(Enumerable.Empty<Env>(), Enumerable.Empty<Plan>());
            }

            var plans = new List<Plan>();
            var envs = new List<Env>();
            
            var finishedEnvironments = false;
            var currentPlan = default(Plan);

            var sb = new StringBuilder();

            foreach (var line in File.ReadAllLines(planFile))
            {
                if (finishedEnvironments)
                {
                    var m = Regex.Match(line, @"^\[(.+)\]$");
                    if (m.Success)
                    {
                        if (currentPlan != default(Plan))
                        {
                            currentPlan.Segments = Segment.Parse(sb.ToString()).ToArray();
                            sb.Clear();
                        }

                        currentPlan = new Plan
                        {
                            Name = m.Result("$1")
                        };

                        plans.Add(currentPlan);
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    if (String.IsNullOrWhiteSpace(line))
                    {
                        finishedEnvironments = true;
                        continue;
                    }

                    var match = Regex.Match(line, @"^(.+):(.+)$");
                    if (!match.Success)
                        throw new Exception(String.Format("Plan file must start with environments in 'name:connectionstring' format (without quotes). '{0}' does not match", line));

                    envs.Add(new Env
                    {
                        Name = match.Result("$1").Trim(),
                        ConnectionString = match.Result("$2").Trim()
                    });
                }

            }

            if (currentPlan != default(Plan))
            {
                currentPlan.Segments = Segment.Parse(sb.ToString()).ToArray();
            }

            return new PlanFile(envs, plans);
        }
    }

    public class UpdateContext
    {
        public Plan Plan { get; set; }
        public IMetabase Metabase { get; set; }
    }

    public class RootPlanPart : PlanPart
    {
        public string Discriminator { get; private set; }
        public string PlanName { get; private set; }

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
            PlanName = context.Plan.Name;
        }

        protected override string XmlClause()
        {
            return String.Format("ROOT('{0}')", PlanName);
        }
    }

    public class LookupPart : SubPart
    {
        public string LookupColumn { get; set; }

        public static IEnumerable<PlanPart> Parse(string text)
        {
            var match = Regex.Match(text, @"([^:\s]+)(?::([^\s]+))?(?:\s(.+))?");

            if (match.Success)
            {
                var descriptor = match.Result("$1");
                var key = match.Result("$2");
                var lookupColumn = match.Result("$3");

                var tableName = descriptor.ToTableName();

                foreach (var elementName in descriptor.ToElementNames())
                    yield return new LookupPart
                    {
                        TableName = tableName,
                        ElementName = elementName,
                        LookupColumn = lookupColumn,
                        ForeignKey = key,
                    };
            }
        }

        //public override string ToQuery()
        //{
        //    return String.Format("(SELECT {0}.{1} FROM {0} {0} WHERE {0}.{2} = {3}.{4}) {5}",
        //        TableName, LookupColumn, PrimaryKey.Name, ParentTable, ForeignKey, ElementName
        //    );
        //}

        protected override void FillIn(PlanPart parent, UpdateContext context)
        {
            base.FillIn(parent, context);

            if (String.IsNullOrEmpty(LookupColumn)) LookupColumn = new[] { 
                "Code", "Name"
            }.FirstOrDefault(x => Columns.Any(y => String.Equals(y.Name, x, StringComparison.CurrentCultureIgnoreCase)));

            Columns = Columns.Where(x => String.Equals(LookupColumn, x.Name, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        }



    }

    public class HasOnePart : SubPart
    {
        public static IEnumerable<PlanPart> Parse(string text)
        {
            foreach (var elementName in text.ToElementNames())
                yield return new HasOnePart { TableName = text.ToTableName(), ElementName = elementName };
        }

        //	public override string ToQuery()
        //	{
        //		return "/* Has One " + TableName + " called " + ElementName + " */";
        //	}
    }

    public class HasManyPart : SubPart
    {
        public static IEnumerable<PlanPart> Parse(string text)
        {
            foreach (var elementName in text.ToElementNames())
                yield return new HasManyPart { TableName = text.ToTableName(), ElementName = elementName };
        }

        protected override void FillIn(PlanPart parent, UpdateContext context)
        {
            if (String.IsNullOrWhiteSpace(ForeignKey)) ForeignKey = parent.Table.Name + parent.Table.PrimaryKey.Name;

            base.FillIn(parent, context);
        }

        protected override string WhereClause()
        {
            return String.Format("WHERE {0}.{1} = {2}.{3}",
                Table.Name, ForeignKey,
                ParentTable.Name, ParentTable.PrimaryKey.Name
            );
        }

        //	public override string ToQuery()
        //	{
        //		return "/* Has Many " + TableName + " called " + ElementName + " */";
        //	}
    }


    public abstract class SubPart : PlanPart
    {
        public TableMeta ParentTable { get; protected set; }
        public string ForeignKey { get; protected set; }

        protected override void FillIn(PlanPart parent, UpdateContext context)
        {
            ParentTable = parent.Table;

            if (String.IsNullOrEmpty(ForeignKey)) ForeignKey = ElementName + Table.PrimaryKey.Name;
        }

        protected override string WhereClause()
        {
            return String.Format("WHERE {0}.{1} = {2}.{3}",
                Table.Name, Table.PrimaryKey.Name,
                ParentTable.Name, ForeignKey
            );
        }

        protected override string QueryTemplate()
        {
            return "(" + base.QueryTemplate() + ")";
        }
    }


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

        //protected virtual IEnumerable<string> 

        protected virtual string XmlClause()
        {
            return "TYPE";
        }

        protected virtual string QueryTemplate()
        {
            return @"SELECT 
{0} 
FROM {2} {3} 
{1} 
FOR XML AUTO, {4}";
        }

        protected virtual string WhereClause()
        {
            return "";
        }

        public virtual string ToQuery()
        {
            var selectClause = String.Join(",\n",
                Columns.Where(x => x.Insertable()).Select(x => x.Name)
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
            FillIn(parent, context);
            foreach (var child in Children)
                child.Update(this, context);
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

    public static class Extensions
    {
        public static string ToTableName(this string s)
        {
            if (s == null) return null;
            return Regex.Replace(s, @"\{[^}]*}", "");
        }

        public static IEnumerable<string> ToElementNames(this string source)
        {
            return Template(source, Regex.Matches(source, @"\{([^{}]*)}").Cast<Match>().ToArray());
        }

        private static IEnumerable<string> Template(string source, Match[] matches)
        {
            if (matches.Length == 0)
                yield return source;
            else
            {
                var head = matches.First();
                var tail = matches.Skip(1).ToArray();
                var items = head.Result("$1").Split(',').Select(x => x.Trim()).ToList();
                var templates = Template(source, tail).ToList();

                foreach (var template in templates)
                    foreach (var item in items)
                        yield return template.Remove(head.Index, head.Length).Insert(head.Index, item);
            }
        }
    }

}
