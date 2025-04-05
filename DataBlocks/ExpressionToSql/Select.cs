using System.Reflection;
using DataBlocks.DataAccess;

namespace ExpressionToSql
{
    using ScheMigrator.Migrations;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using ExpressionToSql.Utils;
    using DataBlocks.ExpressionToSql.Expressions;

    public class Select<T, R> : Query
    {
        private readonly Expression<Func<T, R>> _select;
        private readonly int? _take;
        private readonly Table _table;

        internal Select(Expression<Func<T, R>> select, int? take, Table table, ISqlDialect dialect) 
            : base(dialect)
        {
            _select = select;
            _take = take;
            _table = table;
            
            // Register the primary entity type
            RegisterEntityType(QueryBuilder.TableAliasName, typeof(T));
        }

        public Where<T, R> Where(Expression<Func<T, bool>> predicate)
        {
            return new Where<T, R>(this, predicate);
        }

        public Join<T, T2, R> Join<T2>(DataSchema schema, Expression<Func<T, T2, bool>> joinCondition, JoinType joinType = JoinType.Inner)
        {
            var rightTable = new Table<T2> { Name = schema.CollectionName, Schema = schema.SchemaName };
            return new Join<T, T2, R>(this, rightTable, joinCondition, joinType);
        }

        public Join<T, T2, R> Join<T2>(Table rightTable, Expression<Func<T, T2, bool>> joinCondition, JoinType joinType = JoinType.Inner)
        {
            return new Join<T, T2, R>(this, rightTable, joinCondition, joinType);
        }
        
        public Join<T, T2, R> Join<T2>(string rightTableName, Expression<Func<T, T2, bool>> joinCondition, JoinType joinType = JoinType.Inner)
        {
            var rightTable = new Table<T2> { Name = rightTableName };
            return Join<T2>(rightTable, joinCondition, joinType);
        }

        public Limit<T, R> Limit(int count)
        {
            return new Limit<T, R>(this, count);
        }

        public Page<T, R> Page(int pageNumber, int pageSize)
        {
            return new Page<T, R>(this, pageNumber, pageSize);
        }

        /// <summary>
        /// Adds an OFFSET clause to the query for paging support
        /// </summary>
        /// <param name="offset">The number of rows to skip</param>
        /// <returns>An Offset query object</returns>
        public Offset<T, R> Offset(int offset)
        {
            return new Offset<T, R>(this, offset);
        }

        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            if (_take.HasValue)
            {
                qb.Take(_take.Value);
            }

            var type = _select.Parameters[0].Type;

            var expressions = SelectExpressions.GetExpressions(type, _select.Body);

            SelectExpressions.AddExpressions(expressions, type, qb);

            qb.AddTable(_table);

            return qb;
        }
    }

    public class Select<T1, T2, R>
    {
        internal Select(Expression<Func<T1, T2, R>> select, Expression<Func<T1, T2, bool>> on, int? take, Table table, ISqlDialect dialect)
        {
        }

        public override string ToString()
        {
            return "";
        }
    }
}