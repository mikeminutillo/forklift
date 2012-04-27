using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Forklift
{
    public static class Extensions
    {
        public static string ToTableName(this string s)
        {
            return s == null ? null : Regex.Replace(s, @"\{[^}]*}", "");
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

        public static XElement AsXElement(this IEnumerable<string> source)
        {
            if (source == null) return null;
            var fullText = string.Join("", source);

            return string.IsNullOrWhiteSpace(fullText) ? null : XElement.Parse(fullText);
        }

        public static IDictionary<string, object> ToValues(this XElement element)
        {
            var values = new Dictionary<string, object>();
            foreach (var attribute in element.Attributes())
                values[attribute.Name.ToString()] = attribute.Value;
            return values;
        }
    }

}
