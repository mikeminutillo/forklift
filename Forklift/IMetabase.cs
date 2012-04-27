using System.Collections.Generic;

namespace Forklift
{
    public interface IMetabase
    {
        TableMeta Table(string name);
        IEnumerable<T> Query<T>(string query);
        object Insert(string tableName, IDictionary<string, object> values);
        object Lookup(string tableName, IDictionary<string, object> values);
    }
}