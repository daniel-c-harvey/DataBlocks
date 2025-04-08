using System;
using System.Linq.Expressions;
using System.Text;
using DataBlocks.DataAccess;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// A composite Select query that works with multiple table joins
    /// </summary>
    public class CompositeSelect<TRoot, TResult> : Query
    {
        private readonly Expression<Func<TRoot, TResult>> _selector;
        private readonly Table _rootTable;
        
        internal CompositeSelect(Expression<Func<TRoot, TResult>> selector, Table rootTable, ISqlDialect dialect)
            : base(dialect)
        {
            _selector = selector;
            _rootTable = rootTable;
            
            // Register the root entity type
            RegisterEntityType(QueryBuilder.TableAliasName, typeof(TRoot));
            
            // Register the selector parameters for tracking
            RegisterExpressionParameter(selector);
        }
        
        /// <summary>
        /// Adds a JOIN clause to the query
        /// </summary>
        public CompositeJoin<TRoot, TJoin, TResult> Join<TJoin>(
            DataSchema schema, 
            Expression<Func<TRoot, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            var joinTable = new Table<TJoin> { Name = schema.CollectionName, Schema = schema.SchemaName };
            var baseJoin = new CompositeJoin<TRoot, TResult>(this);
            return baseJoin.Join(joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a JOIN clause to the query with a custom table
        /// </summary>
        public CompositeJoin<TRoot, TJoin, TResult> Join<TJoin>(
            Table joinTable, 
            Expression<Func<TRoot, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            var baseJoin = new CompositeJoin<TRoot, TResult>(this);
            return baseJoin.Join(joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query
        /// </summary>
        public CompositeWhere<TRoot, TResult> Where(Expression<Func<TRoot, bool>> predicate)
        {
            var baseJoin = new CompositeJoin<TRoot, TResult>(this);
            return baseJoin.Where(predicate);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Register aliases with QueryBuilder
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Add the SELECT statement
            CompositeExpressionUtils.AddExpressions(
                CompositeExpressionUtils.GetExpressions(typeof(TResult), _selector.Body),
                typeof(TRoot),
                qb);
            
            // Add the FROM clause
            qb.AddTable(_rootTable, QueryBuilder.TableAliasName);
            
            return qb;
        }
    }
}