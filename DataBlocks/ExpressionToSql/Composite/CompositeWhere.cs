using System;
using System.Linq.Expressions;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Represents a WHERE clause in a composite query with one model
    /// </summary>
    public class CompositeWhere<TRoot, TResult> : Query
    {
        private readonly Query _baseQuery;
        private readonly Expression<Func<TRoot, bool>> _predicate;
        
        internal CompositeWhere(Query baseQuery, Expression<Func<TRoot, bool>> predicate)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            _predicate = predicate;
            
            // Copy entity types from the base query
            CopyEntityTypesFrom(baseQuery);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query
            _baseQuery.ToSql(qb);
            
            // Apply WHERE conditions
            new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where)
                .BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }
    }
    
    /// <summary>
    /// Represents a WHERE clause in a composite query with two models
    /// </summary>
    public class CompositeWhere<TRoot, TJoin, TResult> : Query
    {
        private readonly Query _baseQuery;
        private readonly Expression<Func<TRoot, TJoin, bool>> _predicate;
        
        internal CompositeWhere(Query baseQuery, Expression<Func<TRoot, TJoin, bool>> predicate)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            _predicate = predicate;
            
            // Copy entity types from the base query
            CopyEntityTypesFrom(baseQuery);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query
            _baseQuery.ToSql(qb);
            
            // Apply WHERE conditions
            new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where)
                .BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }
    }
    
    /// <summary>
    /// Represents a WHERE clause in a composite query with three models
    /// </summary>
    public class CompositeWhere<TRoot, TPrevJoin, TJoin, TResult> : Query
    {
        private readonly Query _baseQuery;
        private readonly Expression<Func<TRoot, TPrevJoin, TJoin, bool>> _predicate;
        
        internal CompositeWhere(Query baseQuery, Expression<Func<TRoot, TPrevJoin, TJoin, bool>> predicate)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            _predicate = predicate;
            
            // Copy entity types from the base query
            CopyEntityTypesFrom(baseQuery);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query
            _baseQuery.ToSql(qb);
            
            // Apply WHERE conditions
            new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where)
                .BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }
    }
} 