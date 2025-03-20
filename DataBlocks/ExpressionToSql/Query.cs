namespace ExpressionToSql
{
    using System.Collections.Generic;
    using System.Text;

    public abstract class Query
    {
        protected internal ISqlDialect Dialect { get; private set; }
        protected internal Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        protected Query(ISqlDialect dialect)
        {
            Dialect = dialect;
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