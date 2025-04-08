using System;
using System.Linq.Expressions;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Represents a WHERE clause in a composite query with the root table only
    /// </summary>
    public class CompositeWhere<TRoot, TResult> : Query
    {
        private readonly CompositeJoinBase<TRoot, TResult> _baseJoin;
        private readonly Expression<Func<TRoot, bool>> _predicate;
        
        internal CompositeWhere(CompositeJoinBase<TRoot, TResult> baseJoin, Expression<Func<TRoot, bool>> predicate)
            : base(baseJoin.Dialect)
        {
            _baseJoin = baseJoin;
            _predicate = predicate;
            
            // Copy entity types from the base join
            CopyEntityTypesFrom(baseJoin);
            
            // Register the predicate parameter for tracking
            RegisterExpressionParameter(predicate);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base join query
            _baseJoin.ToSql(qb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Reset the condition state for WHERE clause
            qb.ResetConditionState();
            
            // Build WHERE clause
            var whereExpressionBuilder = new ExpressionBuilder(this, qb).WithClauseType(ClauseType.Where);
            whereExpressionBuilder.BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }
    }
    
    /// <summary>
    /// Represents a WHERE clause in a composite query with the root and one joined table
    /// </summary>
    public class CompositeWhere<TRoot, TJoin, TResult> : Query
    {
        private readonly CompositeJoinBase<TRoot, TResult> _baseJoin;
        private readonly Expression<Func<TRoot, TJoin, bool>> _predicate;
        
        internal CompositeWhere(CompositeJoinBase<TRoot, TResult> baseJoin, Expression<Func<TRoot, TJoin, bool>> predicate)
            : base(baseJoin.Dialect)
        {
            _baseJoin = baseJoin;
            _predicate = predicate;
            
            // Copy entity types from the base join
            CopyEntityTypesFrom(baseJoin);
            
            // Register the predicate parameters for tracking
            RegisterExpressionParameter(predicate);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base join query
            _baseJoin.ToSql(qb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Reset the condition state for WHERE clause
            qb.ResetConditionState();
            
            // Build WHERE clause
            var whereExpressionBuilder = new ExpressionBuilder(this, qb).WithClauseType(ClauseType.Where);
            whereExpressionBuilder.BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }
    }
    
    /// <summary>
    /// Represents a WHERE clause in a composite query with the root and two joined tables
    /// </summary>
    public class CompositeWhere<TRoot, TPrevJoin, TJoin, TResult> : Query
    {
        private readonly CompositeJoinBase<TRoot, TResult> _baseJoin;
        private readonly Expression<Func<TRoot, TPrevJoin, TJoin, bool>> _predicate;
        
        internal CompositeWhere(CompositeJoinBase<TRoot, TResult> baseJoin, Expression<Func<TRoot, TPrevJoin, TJoin, bool>> predicate)
            : base(baseJoin.Dialect)
        {
            _baseJoin = baseJoin;
            _predicate = predicate;
            
            // Copy entity types from the base join
            CopyEntityTypesFrom(baseJoin);
            
            // Register the predicate parameters for tracking
            RegisterExpressionParameter(predicate);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base join query
            _baseJoin.ToSql(qb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Reset the condition state for WHERE clause
            qb.ResetConditionState();
            
            // Build WHERE clause
            var whereExpressionBuilder = new ExpressionBuilder(this, qb).WithClauseType(ClauseType.Where);
            whereExpressionBuilder.BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }
    }
} 