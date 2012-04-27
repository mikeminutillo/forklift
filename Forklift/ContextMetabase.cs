using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;

namespace Forklift
{
    public class ContextMetabase : IMetabase
    {
        private readonly ConcurrentDictionary<string, TableMeta> _metas = new ConcurrentDictionary<string, TableMeta>();
        private readonly DataContext _context;

        public ContextMetabase(DataContext context)
        {
            _context = context;
        }

        public TableMeta Table(string name)
        {
            return _metas.GetOrAdd(name, CreateMeta);

        }

        private TableMeta CreateMeta(string name)
        {
            return new TableMeta
                       {
                           Name = name,
                           Columns = _context.ExecuteQuery<ColumnMeta>(@"
select 
	c.name [Name], 
	t.name [Type], 
	c.max_length [Length], 
	c.is_identity [PrimaryKey], 
	c.precision [Precision], 
	c.scale [Scale], 
	c.is_nullable [Nullable]
from sys.columns c
inner join sys.systypes t ON c.system_type_id = t.xtype and c.user_type_id = t.xusertype
where c.object_id = object_id({0})",
                                                                       name).ToArray()
                       };
        }

        public IEnumerable<T> Query<T>(string query)
        {
            return _context.ExecuteQuery<T>(query);
        }

        public object Lookup(string tableName, IDictionary<string, object> values)
        {
            var lookupCommand = Table(tableName).CreateLookup(values);

            Console.WriteLine(lookupCommand);
            Console.WriteLine();

            var command = _context.Connection.CreateCommand();
            command.CommandText = lookupCommand;
            return command.ExecuteScalar();
        }

        public object Insert(string tableName, IDictionary<string, object> values)
        {
            var insertCommand = Table(tableName).CreateInsert(values);

            Console.WriteLine(insertCommand);
            Console.WriteLine();

            var command = _context.Connection.CreateCommand();
            command.CommandText = insertCommand + "SELECT SCOPE_IDENTITY();";
            return command.ExecuteScalar();
        }
    }
}