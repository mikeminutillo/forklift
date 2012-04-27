using System;
using System.Collections.Generic;
using System.Linq;

namespace Forklift
{
    public class TableMeta
    {
        public string Name { get; set; }
        public ColumnMeta[] Columns { get; set; }

        public ColumnMeta PrimaryKey
        {
            get
            {
                if(Columns.Any() == false)
                    throw new Exception("There is no " + Name + " table!");

                return Columns.SingleOrDefault(c => c.PrimaryKey) 
                    ?? Columns.SingleOrDefault(c => c.IsNamed("Id"))
                    ?? Columns.SingleOrDefault(c => c.IsNamed(Name + "Id"))
                    ?? Columns.First();
            }
        }

        public string CreateInsert(IDictionary<string, object> values)
        {
            var @params1 = (from v in values
                            let c = Columns.SingleOrDefault(x => x.IsNamed(v.Key))
                            select new {c, v}).ToArray();

            Console.WriteLine("I*** {0} ***", Name);
            foreach (var param in @params1)
            {
                Console.WriteLine("I: {0}: {1}", param.c == null ? null : param.c.Name, param.v);
            }

            var @params = (from a in @params1
                           where a.c.Insertable()
                           select new
                                      {
                                          Column = "[" + a.c.Name + "]",
                                          Value = a.c.Stringify(a.v.Value)
                                      }).ToArray();

            return String.Format("INSERT INTO [{0}]({1}) VALUES({2});",
                                 Name,
                                 String.Join(", ", @params.Select(x => x.Column)),
                                 String.Join(", ", @params.Select(x => x.Value))
                );
        }

        public string CreateLookup(IDictionary<string, object> values)
        {
            var @params1 = (from v in values
                           let c = Columns.SingleOrDefault(x => x.IsNamed(v.Key))
                           select new {c, v}).ToArray();

            Console.WriteLine("L*** {0} ***", Name);
            foreach (var param in @params1)
            {
                Console.WriteLine("L: {0}: {1}", param.c == null ? null : param.c.Name, param.v);
            }

            var @params = (from a in @params1
                           where a.c.Insertable()
                           select new
                                      {
                                          Column = "[" + a.c.Name + "]",
                                          Value = a.c.Stringify(a.v.Value)
                                      }).ToArray();

            return String.Format("SELECT [{0}] FROM [{1}] WHERE {2};",
                                 PrimaryKey.Name, Name, String.Join(" AND ", @params.Select(x => String.Format("{0} = {1}", x.Column, x.Value)))
                );
        }
    }
}