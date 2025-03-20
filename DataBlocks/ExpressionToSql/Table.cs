namespace ExpressionToSql
{
    public class Table<T> : Table
    {
        private string _name;

        public override string Name
        {
            get { return _name ?? typeof(T).Name; }
            set { _name = value; }
        }

        public static Table<T> WithSchema(string name)
        {
            return new Table<T> { Schema = name };
        }

        public static Table<T> WithDefaultSchema(ISqlDialect dialect = null)
        {
            return new Table<T> { Schema = dialect?.DefaultSchema };
        }

        /// <summary>
        /// Create a Select query from this table
        /// </summary>
        /// <typeparam name="R">The result type</typeparam>
        /// <param name="selector">The selector expression</param>
        /// <param name="dialect">The SQL dialect to use</param>
        /// <returns>A Select query</returns>
        public Select<T, R> Select<R>(System.Linq.Expressions.Expression<System.Func<T, R>> selector, ISqlDialect dialect = null)
        {
            return new Select<T, R>(selector, null, this, dialect ?? new PostgreSqlDialect());
        }
    }

    public class Table
    {
        public virtual string Name { get; set; }

        public string Schema { get; set; }
    }
}