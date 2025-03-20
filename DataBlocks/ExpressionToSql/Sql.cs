namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;

    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public static class Sql
    {
        // Default to MS SQL dialect
        private static ISqlDialect DefaultDialect = new TSqlDialect();

        #region Generic (defaults to MSSQL)
        public static Select<T, R> Select<T, R>(Expression<Func<T, R>> selector, string tableName = null)
        {
            return Create(selector, null, tableName, DefaultDialect);
        }

        public static Select<T1, T2, R> Select<T1, T2, R>(Expression<Func<T1, T2, R>> selector, Expression<Func<T1, T2, bool>> on, string tableName = null)
        {
            throw new NotImplementedException("TODO");
            // return new Select<T1, T2, R>(selector, on, null, new Table<T1> { Name = tableName });
        }

        public static Top<T, R> Top<T, R>(Expression<Func<T, R>> selector, int take, string tableName = null)
        {
            return new Top<T, R>(selector, take, new Table<T> { Name = tableName, Schema = DefaultDialect.DefaultSchema }, DefaultDialect);
        }

        // Made public to be called from extension methods
        public static Select<T, R> Create<T, R>(Expression<Func<T, R>> selector, int? take, string tableName, ISqlDialect dialect)
        {
            return Create(selector, take, new Table<T> {Name = tableName, Schema = dialect.DefaultSchema}, dialect);
        }

        public static Select<T, R> Select<T, R>(Expression<Func<T, R>> selector, Table<T> table)
        {
            return Create(selector, null, table, DefaultDialect);
        }

        // public static Top<T, R> Top<T, R>(Expression<Func<T, R>> selector, int take, Table<T> table)
        // {
        //     return new Top<T, R>(selector, take, table, DefaultDialect);
        // }

        public static Select<T, R> Select<T, R>(Expression<Func<T, R>> selector, Table table)
        {
            return Create(selector, null, table, DefaultDialect);
        }

        public static Top<T, R> Top<T, R>(Expression<Func<T, R>> selector, int take, Table table)
        {
            return new Top<T, R>(selector, take, table, DefaultDialect);
        }

        // Made public to be called from extension methods
        public static Select<T, R> Create<T, R>(Expression<Func<T, R>> selector, int? take, Table table, ISqlDialect dialect)
        {
            return new Select<T, R>(selector, take, table, dialect);
        }
        #endregion

        // Configure the default dialect to use
        public static void SetDefaultDialect(ISqlDialect dialect)
        {
            DefaultDialect = dialect ?? new TSqlDialect();
        }
    }
}
