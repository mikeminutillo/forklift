using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Forklift
{
    public class LookupPart : SubPart
    {
        public string LookupColumn { get; set; }

        public static IEnumerable<PlanPart> Parse(string text)
        {
            var match = Regex.Match(text, @"([^:\s]+)(?::([^\s]+))?(?:\s+(.+))?");

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

        public bool CanBeInlined()
        {
            if(Children.Any()) return false;
            return ExtractableColumns().Count() == 1;
        }

        public override string ToQuery()
        {
            if (!CanBeInlined()) 
                return base.ToQuery();

            return String.Format("(SELECT [{0}].[{1}] FROM [{0}] [{0}] WHERE [{0}].[{2}] = [{3}].[{4}]) [{5}]",
                Table.Name, LookupColumn, Table.PrimaryKey.Name, ParentElementName, ForeignKey, ElementName
            );
        }

        protected override void FillIn(PlanPart parent, UpdateContext context)
        {
            base.FillIn(parent, context);

            if (String.IsNullOrEmpty(LookupColumn)) 
                LookupColumn = new[] { "Code", "Name"}.FirstOrDefault(x => Columns.Any(y => y.IsNamed(x)));

            if(String.IsNullOrWhiteSpace(LookupColumn))
            {
                throw new Exception("No columns specified for lookup!");
            }

            var lookups = Regex.Split(LookupColumn, @"\s").Select(x => x.Trim()).ToArray();
            var ignored = Columns.Where(x => lookups.All(y => x.IsNamed(y) == false));

            Ignore(ignored.ToArray());
            parent.Ignore(ForeignKey);
        }

        protected override void UpdateParentValues(IDictionary<string, object> parentValues, object key)
        {
            var canBeInlined = CanBeInlined();
            Console.WriteLine("LOOKUP: {0} {1}", TableName, canBeInlined);
            if(canBeInlined)
            {
                Console.WriteLine("Removing {0}", ElementName);
                parentValues.Remove(ElementName);
            }
            base.UpdateParentValues(parentValues, key);
        }

        protected override object PushSelf(IMetabase metabase, IDictionary<string, object> values)
        {
            var value = metabase.Lookup(Table.Name, values);
            Console.WriteLine("Looked up: {0}", value);
            return value;
        }

        protected override IEnumerable<XElement> GetElements(XElement parent)
        {
            return CanBeInlined() ? new[] {parent} : base.GetElements(parent);
        }

        protected override IDictionary<string, object> GetValues(XElement element)
        {
            if(CanBeInlined())
            {
                var values = from a in element.Attributes(ElementName)
                              select a.Value;
                var dictionary = new Dictionary<string, object>();

                foreach(var value in values)
                    dictionary[LookupColumn] = value;

                return dictionary;

            }
            return base.GetValues(element);
        }
    }
}