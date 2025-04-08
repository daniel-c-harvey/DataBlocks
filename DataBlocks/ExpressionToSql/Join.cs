using System;
using System.Linq.Expressions;
using System.Text;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql
{
    public enum JoinType
    {
        Inner,
        Left,
        Right,
        Full
    }

    public class Join<T1, T2, R> : Query
    {
        private readonly Select<T1, R> _select;
        private readonly Table _rightTable;
        private readonly Expression<Func<T1, T2, bool>> _joinCondition;
        private readonly JoinType _joinType;
        private readonly QueryBuilder _queryBuilder;
        private readonly ExpressionBuilder _expressionBuilder;
        private string _rightTableAlias;

        internal Join(Select<T1, R> select, Table rightTable, Expression<Func<T1, T2, bool>> joinCondition, JoinType joinType = JoinType.Inner)
            : base(select.Dialect)
        {
            _select = select;
            _rightTable = rightTable;
            _joinCondition = joinCondition;
            _joinType = joinType;
            _queryBuilder = new QueryBuilder(new StringBuilder(), select.Dialect, this);
            _expressionBuilder = new ExpressionBuilder(this, _queryBuilder).WithClauseType(ClauseType.On);
            
            // Copy entity types from the select query
            CopyEntityTypesFrom(select);
            
            // Register both parameters from the join expression
            RegisterExpressionParameter(joinCondition);
        }

        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First build the base query
            _select.ToSql(qb);
            
            // Apply entity types from base query
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Register T1 as the primary table 
            qb.RegisterTableAlias<T1>(QueryBuilder.TableAliasName);
            
            // Register T2 as the join table and get its alias
            _rightTableAlias = qb.GetNextAlias();
            qb.RegisterTableAlias<T2>(_rightTableAlias);
            
            // Register the join entity type with the QueryBuilder
            RegisterEntityType(_rightTableAlias, typeof(T2));
            
            // Add JOIN clause
            qb.AppendJoin(_joinType.ToSqlString(), _rightTable, _rightTableAlias);
            
            // Reset the first condition flag for ON clause
            qb.ResetConditionState();
            
            // Create a new expression builder with ON clause type
            var joinExpressionBuilder = new ExpressionBuilder(this, qb).WithClauseType(ClauseType.On);
            joinExpressionBuilder.BuildExpression(_joinCondition.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }
        
        public Where<T1, R> Where(Expression<Func<T1, bool>> predicate)
        {
            return new Where<T1, R>(this, predicate);
        }
    }
} 