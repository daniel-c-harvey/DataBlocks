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
    public abstract class CompositeJoinBase<TRoot> : QueryRoot<TRoot>
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

        internal readonly QueryRoot<TRoot> BaseQuery;
        internal readonly List<IJoinInfo> Joins = new List<IJoinInfo>();
        
        internal CompositeJoinBase(QueryRoot<TRoot> baseQuery)
            : base(baseQuery.Dialect)
        {
            BaseQuery = baseQuery;
            CopyAliasesFrom(baseQuery);
        }
        
        /// <summary>
        /// Handles the registration of aliases for a join
        /// </summary>
        protected void RegisterJoinAlias(IJoinInfo join, QueryBuilder qb)
        {
            // First register the join (right) type
            string rightAlias = Aliases.RegisterType(join.JoinType);
            join.TableAlias = rightAlias;
            
            string leftAlias = Aliases.GetAliasForType(join.LeftType);
            
            // Register the mappings in the QueryBuilder
            qb.RegisterTableAliasForType(join.JoinType, rightAlias);
            qb.RegisterTableAliasForType(join.LeftType, leftAlias);
            
            // Check if this is a join against the root type and store alias mappings if needed
            if (join.LeftType == typeof(TRoot))
            {
                // If we're joining against the root type, ensure any default aliases are mapped
                if (leftAlias != QueryBuilder.TableAliasName)
                {
                    // Tell the AliasRegistry about this redirection
                    Aliases.RedirectAlias(QueryBuilder.TableAliasName, leftAlias);
                    
                    // Also tell the QueryBuilder for backward compatibility
                    qb.StoreAliasMapping(QueryBuilder.TableAliasName, leftAlias);
                }
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
            
            // If this is a join involving the root table, set the proper root alias
            if (join.LeftType == typeof(TRoot))
            {
                string rootAlias = qb.GetEffectiveAlias(qb.GetAliasForType(typeof(TRoot)) ?? QueryBuilder.TableAliasName);
                joinExpressionBuilder.WithRootAlias(rootAlias);
            }
            
            join.BuildJoinCondition(joinExpressionBuilder);
            
            // Check if this JOIN involves the root table
            if (join.LeftType == typeof(TRoot))
            {
                // Get the current alias for the root type (might be a subquery alias)
                string rootAlias = qb.GetAliasForType(typeof(TRoot));
                
                // Get the effective alias with any mappings applied
                rootAlias = qb.GetEffectiveAlias(rootAlias ?? QueryBuilder.TableAliasName);
                
                // If the root alias is different from the default table alias,
                // we need to update all references in the join condition
                if (rootAlias != QueryBuilder.TableAliasName)
                {
                    // Replace all occurrences of the default table alias with the actual alias
                    qb.ReplaceAliasInJoinCondition(QueryBuilder.TableAliasName, rootAlias);
                }
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            if (BaseQuery != null)
            {
                BaseQuery.ToSql(qb);
            }
            else
            {
                throw new InvalidOperationException("No base query available in CompositeJoinBase");
            }
            
            // Apply entity types to ensure aliases are properly registered
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Make sure TRoot is registered with the primary alias if not already registered
            string rootAlias = qb.GetAliasForType(typeof(TRoot));
            if (string.IsNullOrEmpty(rootAlias))
            {
                qb.RegisterTableAliasForType(typeof(TRoot), QueryBuilder.TableAliasName);
                RegisterEntityType(QueryBuilder.TableAliasName, typeof(TRoot));
            }
            else if (rootAlias != QueryBuilder.TableAliasName)
            {
                // If the root is registered with a different alias (like in a subquery),
                // ensure we have an alias mapping for any references to the default alias
                qb.StoreAliasMapping(QueryBuilder.TableAliasName, rootAlias);
            }
            
            // Register parameters from all joins
            foreach (var join in Joins)
            {
                join.RegisterParameters(this);
            }
            
            // Process each join in order
            foreach (var join in Joins)
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
        
        internal CompositeJoin(CompositePageByRootBase<TRoot> baseQuery)
            : base(baseQuery)
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
            result.Joins.AddRange(Joins);
            result.Joins.Add(new JoinInfo<TRoot, TJoin>(joinTable, joinCondition, joinType));
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
            : base(baseJoin.BaseQuery)
        {
            CopyEntityTypesFrom(baseJoin);
            Joins.AddRange(baseJoin.Joins);
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
            result.Joins.Add(new JoinInfo<TJoin1, TJoin2>(joinTable, joinCondition, joinType));
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
            : base(baseJoin.BaseQuery)
        {
            CopyEntityTypesFrom(baseJoin);
            Joins.AddRange(baseJoin.Joins);
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
            : base(baseJoin.BaseQuery)
        {
            CopyEntityTypesFrom(baseJoin);
            Joins.AddRange(baseJoin.Joins);
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