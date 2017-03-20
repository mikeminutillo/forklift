using System;

namespace Forklift
{
    public class ColumnMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public short Length { get; set; }
        public bool PrimaryKey { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public bool Nullable { get; set; }

        public bool IsNamed(string name)
        {
            return String.Equals(name, Name, StringComparison.CurrentCultureIgnoreCase);
        }

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
                case "uniqueidentifier":
                case "nvarchar":
                case "varchar":
                    return "'" + String.Format("{0}", o).Replace("'", "''") + "'";
                case "datetime":
                    return "'" + Convert.ToDateTime(o).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                case "datetimeoffset":
                    return "'" + o + "'";
                default:
                    return String.Format("{0}", o);
            }
        }
    }


}
