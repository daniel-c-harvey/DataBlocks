using System;
using System.Linq.Expressions;
using System.Text;
using ExpressionToSql;
using DataBlocks.ExpressionToSql.Expressions;
using ScheMigrator.Migrations;

namespace DataBlocks.ExpressionToSql.Expressions
{
    public abstract class ConditionBuilder<T> : Query
    {
        protected readonly QueryBuilder _queryBuilder;
        protected readonly ExpressionBuilder _expressionBuilder;

        protected ConditionBuilder(Query query, QueryBuilder queryBuilder) 
            : base(query.Dialect)
        {
            _queryBuilder = queryBuilder;
            _expressionBuilder = new ExpressionBuilder(this, queryBuilder);
        }

        protected void BuildExpression(Expression expression)
        {
            if (expression is LambdaExpression lambda)
            {
                _expressionBuilder.BuildExpression(lambda.Body, ExpressionBuilder.Clause.And);
            }
            else
            {
                _expressionBuilder.BuildExpression(expression, ExpressionBuilder.Clause.And);
            }
        }
    }
}