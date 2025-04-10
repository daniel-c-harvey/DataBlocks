using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DataBlocks.DataAccess;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Represents a base JOIN type in a composite query 
    /// </summary>
    public abstract class CompositeJoinBase<TRoot> : Query
    {
        /// <summary>
        /// Interface for type-safe join information
        /// </summary>
        public interface IJoinInfo
        {
            Type LeftType { get; }
            Type JoinType { get; }
            Table JoinTable { get; }
            JoinType JoinOperationType { get; }
            string TableAlias { get; set; }
            
            // Expression parameter registration is handled at the Query level
            void RegisterParameters(Query query);
            
            // Building the join condition is a separate concern
            void BuildJoinCondition(ExpressionBuilder expressionBuilder);
        }
        
        /// <summary>
        /// Strongly-typed join information
        /// </summary>
        public class JoinInfo<TLeft, TRight> : IJoinInfo
        {
            public Type LeftType => typeof(TLeft);
            public Type JoinType => typeof(TRight);
            public Table JoinTable { get; }
            public Expression<Func<TLeft, TRight, bool>> JoinCondition { get; }
            public JoinType JoinOperationType { get; }
            public string TableAlias { get; set; }
            
            public JoinInfo(Table joinTable, Expression<Func<TLeft, TRight, bool>> joinCondition, JoinType joinOperationType)
            {
                JoinTable = joinTable;
                JoinCondition = joinCondition;
                JoinOperationType = joinOperationType;
            }
            
            public void RegisterParameters(Query query)
            {
                // Register parameters from the join condition
                query.RegisterExpressionParameter(JoinCondition);
            }
            
            public void BuildJoinCondition(ExpressionBuilder expressionBuilder)
            {
                // Build the actual condition
                expressionBuilder.BuildExpression(JoinCondition.Body, ExpressionBuilder.Clause.And);
            }
        }
        
        public readonly CompositeFrom<TRoot> _baseQuery;
        public readonly CompositePageByRootBase<TRoot> _pageByRootBaseQuery;
        public readonly List<IJoinInfo> _joins = new List<IJoinInfo>();
        
        internal CompositeJoinBase(CompositeFrom<TRoot> baseQuery)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            CopyEntityTypesFrom(baseQuery);
        }
        
        internal CompositeJoinBase(CompositePageByRootBase<TRoot> pageByRootBaseQuery)
            : base(pageByRootBaseQuery.Dialect)
        {
            _pageByRootBaseQuery = pageByRootBaseQuery;
            CopyEntityTypesFrom(pageByRootBaseQuery);
        }
        
        /// <summary>
        /// Gets all entity types used in this join, with TRoot first
        /// </summary>
        public IEnumerable<Type> GetEntityTypes()
        {
            yield return typeof(TRoot);
            foreach (var join in _joins)
            {
                yield return join.JoinType;
            }
        }
        
        /// <summary>
        /// Handles the registration of aliases for a join
        /// </summary>
        protected void RegisterJoinAlias(IJoinInfo join, QueryBuilder qb)
        {
            // Use the improved GetOrCreateAliasForType method
            join.TableAlias = qb.GetOrCreateAliasForType(join.JoinType);
            
            // Register the join entity type in our Query object if not already there
            if (!EntityTypes.ContainsKey(join.TableAlias))
            {
                RegisterEntityType(join.TableAlias, join.JoinType);
            }
        }
        
        /// <summary>
        /// Handles the building of a JOIN clause
        /// </summary>
        protected void BuildJoinClause(IJoinInfo join, QueryBuilder qb)
        {
            // Add JOIN clause
            qb.AppendJoin(join.JoinOperationType.ToSqlString(), join.JoinTable, join.TableAlias);
            
            // Reset condition state for ON clause
            qb.ResetConditionState();
            
            // Build the join condition with the strongly-typed builder
            var joinExpressionBuilder = new ExpressionBuilder(this, qb).WithClauseType(ClauseType.On);
            join.BuildJoinCondition(joinExpressionBuilder);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            if (_baseQuery != null)
            {
                _baseQuery.ToSql(qb);
            }
            else if (_pageByRootBaseQuery != null)
            {
                _pageByRootBaseQuery.ToSql(qb);
            }
            else
            {
                throw new InvalidOperationException("No base query available in CompositeJoinBase");
            }
            
            // Apply entity types to ensure aliases are properly registered
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Make sure TRoot is registered with the primary alias
            qb.RegisterTableAlias<TRoot>(QueryBuilder.TableAliasName);
            RegisterEntityType(QueryBuilder.TableAliasName, typeof(TRoot));
            
            // Register parameters from all joins
            foreach (var join in _joins)
            {
                join.RegisterParameters(this);
            }
            
            // Process each join in order
            foreach (var join in _joins)
            {
                RegisterJoinAlias(join, qb);
                BuildJoinClause(join, qb);
            }
            
            return qb;
        }
    }
    
    /// <summary>
    /// Represents the first JOIN in a composite query
    /// </summary>
    public class CompositeJoin<TRoot> : CompositeJoinBase<TRoot>
    {
        internal CompositeJoin(CompositeFrom<TRoot> baseQuery)
            : base(baseQuery)
        {
        }
        
        internal CompositeJoin(CompositePageByRootBase<TRoot> pageByRootBaseQuery)
            : base(pageByRootBaseQuery)
        {
        }
        
        /// <summary>
        /// Adds a JOIN to the query with the root table
        /// </summary>
        public CompositeJoin<TRoot, TJoin> Join<TJoin>(
            Table joinTable,
            Expression<Func<TRoot, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            var result = new CompositeJoin<TRoot, TJoin>(this);
            result._joins.AddRange(_joins);
            result._joins.Add(new JoinInfo<TRoot, TJoin>(joinTable, joinCondition, joinType));
            return result;
        }
        
        /// <summary>
        /// Adds a JOIN to the query with a schema-based table
        /// </summary>
        public CompositeJoin<TRoot, TJoin> Join<TJoin>(
            DataSchema schema,
            Expression<Func<TRoot, TJoin, bool>> joinCondition,
            JoinType joinType = JoinType.Inner)
        {
            var joinTable = new Table<TJoin> { Name = schema.CollectionName, Schema = schema.SchemaName };
            return Join(joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root table only
        /// </summary>
        public CompositeWhere<TRoot> Where(Expression<Func<TRoot, bool>> predicate)
        {
            return new CompositeWhere<TRoot>(this, predicate);
        }
    }
    
    /// <summary>
    /// Represents a JOIN chain with one previous join
    /// </summary>
    public class CompositeJoin<TRoot, TJoin1> : CompositeJoinBase<TRoot>
    {
        internal CompositeJoin(CompositeJoinBase<TRoot> baseJoin)
            : base(baseJoin._baseQuery)
        {
            CopyEntityTypesFrom(baseJoin);
            _joins.AddRange(baseJoin._joins);
        }
        
        /// <summary>
        /// Adds a subsequent JOIN to the query
        /// </summary>
        public CompositeJoin<TRoot, TJoin1, TJoin2> Join<TJoin2>(
            Table joinTable,
            Expression<Func<TJoin1, TJoin2, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            // First join should be established at this point
            var result = new CompositeJoin<TRoot, TJoin1, TJoin2>(this);
            result._joins.Add(new JoinInfo<TJoin1, TJoin2>(joinTable, joinCondition, joinType));
            return result;
        }
        
        /// <summary>
        /// Adds a subsequent JOIN to the query with a schema-based table
        /// </summary>
        public CompositeJoin<TRoot, TJoin1, TJoin2> Join<TJoin2>(
            DataSchema schema,
            Expression<Func<TJoin1, TJoin2, bool>> joinCondition,
            JoinType joinType = JoinType.Inner)
        {
            var joinTable = new Table<TJoin2> { Name = schema.CollectionName, Schema = schema.SchemaName };
            return Join(joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root table only
        /// </summary>
        public CompositeWhere<TRoot> Where(Expression<Func<TRoot, bool>> predicate)
        {
            return new CompositeWhere<TRoot>(this, predicate);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root and one joined table
        /// </summary>
        public CompositeWhere<TRoot, TJoin1> Where(Expression<Func<TRoot, TJoin1, bool>> predicate)
        {
            return new CompositeWhere<TRoot, TJoin1>(this, predicate);
        }
    }
    
    /// <summary>
    /// Represents a JOIN chain with two previous joins
    /// </summary>
    public class CompositeJoin<TRoot, TJoin1, TJoin2> : CompositeJoinBase<TRoot>
    {
        internal CompositeJoin(CompositeJoinBase<TRoot> baseJoin)
            : base(baseJoin._baseQuery)
        {
            CopyEntityTypesFrom(baseJoin);
            _joins.AddRange(baseJoin._joins);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root table only
        /// </summary>
        public CompositeWhere<TRoot> Where(Expression<Func<TRoot, bool>> predicate)
        {
            return new CompositeWhere<TRoot>(this, predicate);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root and one joined table
        /// </summary>
        public CompositeWhere<TRoot, TJoin1> Where(Expression<Func<TRoot, TJoin1, bool>> predicate)
        {
            return new CompositeWhere<TRoot, TJoin1>(this, predicate);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root and two joined tables
        /// </summary>
        public CompositeWhere<TRoot, TJoin1, TJoin2> Where(Expression<Func<TRoot, TJoin1, TJoin2, bool>> predicate)
        {
            return new CompositeWhere<TRoot, TJoin1, TJoin2>(this, predicate);
        }
    }
    
    /// <summary>
    /// Represents a JOIN chain with three previous joins
    /// </summary>
    public class CompositeJoin<TRoot, TJoin1, TJoin2, TJoin3> : CompositeJoinBase<TRoot>
    {
        internal CompositeJoin(CompositeJoinBase<TRoot> baseJoin)
            : base(baseJoin._baseQuery)
        {
            CopyEntityTypesFrom(baseJoin);
            _joins.AddRange(baseJoin._joins);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root table only
        /// </summary>
        public CompositeWhere<TRoot> Where(Expression<Func<TRoot, bool>> predicate)
        {
            return new CompositeWhere<TRoot>(this, predicate);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root and one joined table
        /// </summary>
        public CompositeWhere<TRoot, TJoin1> Where(Expression<Func<TRoot, TJoin1, bool>> predicate)
        {
            return new CompositeWhere<TRoot, TJoin1>(this, predicate);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root and two joined tables
        /// </summary>
        public CompositeWhere<TRoot, TJoin1, TJoin2> Where(Expression<Func<TRoot, TJoin1, TJoin2, bool>> predicate)
        {
            return new CompositeWhere<TRoot, TJoin1, TJoin2>(this, predicate);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query with root and three joined tables
        /// </summary>
        public CompositeWhere<TRoot, TJoin1, TJoin2, TJoin3> Where(Expression<Func<TRoot, TJoin1, TJoin2, TJoin3, bool>> predicate)
        {
            return new CompositeWhere<TRoot, TJoin1, TJoin2, TJoin3>(this, predicate);
        }
    }
} 