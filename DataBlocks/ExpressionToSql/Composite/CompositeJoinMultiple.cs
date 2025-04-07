using System;
using System.Linq.Expressions;
using DataBlocks.DataAccess;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Represents a JOIN with multiple tables in a composite query
    /// </summary>
    public class CompositeJoin<TRoot, TPrevJoin, TJoin, TResult> : Query
    {
        private readonly Query _baseJoin;
        private readonly Table _joinTable;
        private readonly Expression<Func<TPrevJoin, TJoin, bool>> _joinCondition;
        private readonly JoinType _joinType;
        private string _joinTableAlias;
        
        internal CompositeJoin(
            Query baseJoin, 
            Table joinTable, 
            Expression<Func<TPrevJoin, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
            : base(baseJoin.Dialect)
        {
            _baseJoin = baseJoin;
            _joinTable = joinTable;
            _joinCondition = joinCondition;
            _joinType = joinType;
            
            // Copy entity types from the base join
            CopyEntityTypesFrom(baseJoin);
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
            return new CompositeJoin<TRoot, TJoin, TNextJoin, TResult>(
                this, nextJoinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds another JOIN to the query with a custom table
        /// </summary>
        public CompositeJoin<TRoot, TJoin, TNextJoin, TResult> Join<TNextJoin>(
            Table joinTable,
            Expression<Func<TJoin, TNextJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            return new CompositeJoin<TRoot, TJoin, TNextJoin, TResult>(
                this, joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query
        /// </summary>
        public CompositeWhere<TRoot, TPrevJoin, TJoin, TResult> Where(
            Expression<Func<TRoot, TPrevJoin, TJoin, bool>> predicate)
        {
            return new CompositeWhere<TRoot, TPrevJoin, TJoin, TResult>(this, predicate);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base join query
            _baseJoin.ToSql(qb);
            
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