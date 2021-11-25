using System;

namespace MiniORM.Attributes
{
    public class KeyAttribute : Attribute
    {
        public string TableName { get; }
        public string PKName { get; }
        public string FKName { get; }
        public KeyAttribute(string t, string p, string f)
        {
            TableName = t;
            PKName = p;
            FKName = f;
        }
    }
}
