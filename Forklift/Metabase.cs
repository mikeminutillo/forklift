using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using System.Collections.Concurrent;

namespace Forklift
{
    public interface IMetabase
    {
        TableMeta Table(string name);
    }

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
    }

    public class TableMeta
    {
        public string Name { get; set; }
        public ColumnMeta[] Columns { get; set; }

        public ColumnMeta PrimaryKey
        {
            get { return Columns.SingleOrDefault(c => c.PrimaryKey) ?? Columns.SingleOrDefault(c => String.Equals(c.Name, "Id", StringComparison.CurrentCultureIgnoreCase)) ?? Columns.First(); }
        }

        public string CreateInsert(IDictionary<string, object> values)
        {
            var @params = (from v in values
                           let c = Columns.Single(x => String.Equals(v.Key, x.Name))
                           where c.Insertable()
                           select new
                           {
                               Column = "[" + c.Name + "]",
                               Value = c.Stringify(v.Value)
                           }).ToArray();

            return String.Format("INSERT INTO [{0}]({1}) VALUES({2});",
                                 Name,
                                 String.Join(", ", @params.Select(x => x.Column)),
                                 String.Join(", ", @params.Select(x => x.Value))
                );
        }

        public string CreateLookup(IDictionary<string, object> values)
        {
            var @params = (from v in values
                           let c = Columns.Single(x => String.Equals(v.Key, x.Name))
                           where c.Insertable()
                           select new
                           {
                               Column = "[" + c.Name + "]",
                               Value = c.Stringify(v.Value)
                           }).ToArray();

            return String.Format("SELECT [{0}] FROM [{1}] WHERE {2};",
                                 PrimaryKey.Name, Name, String.Join(" AND ", @params.Select(x => String.Format("{0} = {1}", x.Column, x.Value)))
                );
        }
    }

    public class ColumnMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public short Length { get; set; }
        public bool PrimaryKey { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public bool Nullable { get; set; }


        public bool Insertable()
        {
            switch (Type)
            {
                case "timestamp":
                    return false;
            }
            return true;
        }

        public string Stringify(object o)
        {
            if (o == null)
                if (Nullable)
                    return "NULL";
                else
                    throw new Exception(String.Format("Attempt to store null value in non null column: {0}", Name));

            switch (Type)
            {
                case "nvarchar":
                case "varchar":
                    return "'" + String.Format("{0}", o).Replace("'", "''") + "'";
                case "datetime":
                    return "'" + Convert.ToDateTime(o).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                default:
                    return String.Format("{0}", o);
            }
        }
    }


}
