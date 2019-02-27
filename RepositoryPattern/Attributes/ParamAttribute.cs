using System;
using System.Data;

namespace RepositoryPattern.Attributes
{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple = false)]
    public class ParamAttribute : Attribute
    {
        public SqlDbType Type { get; private set; }
        public int? Size { get; private set; }
        public string Name { get; private set; }
        public ParameterDirection Direction { get; private set; }
        public ParamAttribute(string name,SqlDbType type, ParameterDirection direction = ParameterDirection.Input)
        {
            this.Name = name;
            this.Type = type;
            this.Direction = direction;
        }

        public ParamAttribute(string name, SqlDbType type,int size, ParameterDirection direction = ParameterDirection.Input)
            :this(name,type,direction)
        {
            this.Size = size;
        }
    }
}
