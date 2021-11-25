using System;

namespace MiniORM.Attributes
{
    public class DataFieldAttribute : Attribute
    {
        public string Name { get; }
        public string Type { get; }
        public DataFieldAttribute(string n, string t)
        {
            Name = n;
            Type = t;
        }
    }
}
