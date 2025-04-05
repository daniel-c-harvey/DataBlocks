namespace ExpressionToSql
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public abstract class Query
    {
        protected internal ISqlDialect Dialect { get; private set; }
        protected internal Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        
        // Dictionary to track entity types by alias
        protected internal Dictionary<string, Type> EntityTypes { get; } = new Dictionary<string, Type>();

        protected Query(ISqlDialect dialect)
        {
            Dialect = dialect;
        }
        
        // Method to register an entity type for an alias
        protected internal void RegisterEntityType(string alias, Type entityType)
        {
            EntityTypes[alias] = entityType;
        }
        
        // Method to retrieve the entity type for an alias
        protected internal Type GetEntityType(string alias)
        {
            return EntityTypes.TryGetValue(alias, out var type) ? type : null;
        }
        
        // Method to copy entity types from another query
        protected internal void CopyEntityTypesFrom(Query sourceQuery)
        {
            foreach (var pair in sourceQuery.EntityTypes)
            {
                EntityTypes[pair.Key] = pair.Value;
            }
        }

        public override string ToString()
        {
            return ToSql();
        }
        
        public string ToSql()
        {
            return ToSql(new StringBuilder()).ToString();
        }

        public StringBuilder ToSql(StringBuilder sb)
        {
            ToSql(new QueryBuilder(sb, Dialect, this));
            return sb;
        }

        internal abstract QueryBuilder ToSql(QueryBuilder qb);
    }
}