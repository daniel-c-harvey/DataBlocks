using System;
using System.Linq.Expressions;
using DataBlocks.DataAccess;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Represents a JOIN in a composite query
    /// </summary>
    public class CompositeJoin<TRoot, TJoin, TResult> : Query
    {
        private readonly Query _baseQuery;
        private readonly Table _joinTable;
        private readonly Expression<Func<TRoot, TJoin, bool>> _joinCondition;
        private readonly JoinType _joinType;
        private string _joinTableAlias;
        
        internal CompositeJoin(
            CompositeSelect<TRoot, TResult> baseSelect, 
            Table joinTable, 
            Expression<Func<TRoot, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
            : base(baseSelect.Dialect)
        {
            _baseQuery = baseSelect;
            _joinTable = joinTable;
            _joinCondition = joinCondition;
            _joinType = joinType;
            
            // Copy entity types from the select query
            CopyEntityTypesFrom(baseSelect);
        }
        
        /// <summary>
        /// Adds another JOIN to the query
        /// </summary>
        public CompositeJoin<TRoot, TJoin, TNextJoin, TResult> Join<TNextJoin>(
            DataSchema schema, 
            Expression<Func<TJoin, TNextJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            var nextJoinTable = new Table<TNextJoin> { Name = schema.CollectionName, Schema = schema.SchemaName };
            return new CompositeJoin<TRoot, TJoin, TNextJoin, TResult>(this, nextJoinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds another JOIN to the query with a custom table
        /// </summary>
        public CompositeJoin<TRoot, TJoin, TNextJoin, TResult> Join<TNextJoin>(
            Table joinTable,
            Expression<Func<TJoin, TNextJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            return new CompositeJoin<TRoot, TJoin, TNextJoin, TResult>(this, joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query
        /// </summary>
        public CompositeWhere<TRoot, TJoin, TResult> Where(Expression<Func<TRoot, TJoin, bool>> predicate)
        {
            return new CompositeWhere<TRoot, TJoin, TResult>(this, predicate);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base select query
            _baseQuery.ToSql(qb);
            
            // Register TRoot as the primary table 
            qb.RegisterTableAlias<TRoot>(QueryBuilder.TableAliasName);
            
            // Register TJoin as the join table and get its alias
            _joinTableAlias = qb.GetNextAlias();
            qb.RegisterTableAlias<TJoin>(_joinTableAlias);
            
            // Register the join entity type
            RegisterEntityType(_joinTableAlias, typeof(TJoin));
            
            // Add JOIN clause
            qb.AppendJoin(_joinType.ToSqlString(), _joinTable, _joinTableAlias);
            
            // Reset condition state for ON clause
            qb.ResetConditionState();
            
            // Build the join condition
            var joinExpressionBuilder = new ExpressionBuilder(this, qb).WithClauseType(ClauseType.On);
            joinExpressionBuilder.BuildExpression(_joinCondition.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }
    }
} 