using System;

namespace ScheMigrator.Migrations
{

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ScheDataAttribute : Attribute
    {
        public string FieldName { get; }
        public bool IsNullable { get; } = false;
        public bool IsPrimaryKey { get; protected set; } = false;

        public ScheDataAttribute(string name)
        {
            FieldName = name;
        }

        public ScheDataAttribute(string name, bool isNullable)
        {
            FieldName = name;
            IsNullable = isNullable;
        }
    }

    public class ScheKeyAttribute : ScheDataAttribute
    {
        public ScheKeyAttribute(string name) : base(name, false)
        {
            IsPrimaryKey = true;
        }
    }
}