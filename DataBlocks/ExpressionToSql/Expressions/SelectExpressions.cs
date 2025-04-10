using System;
using System.Linq.Expressions;
using ExpressionToSql;
using ExpressionToSql.Composite;
using ExpressionToSql.Utils;
using ScheMigrator.Migrations;

namespace DataBlocks.ExpressionToSql.Expressions;

internal static class SelectExpressions
{
    public static IEnumerable<Expression> GetExpressions(Type type, Expression body)
    {
        return CompositeExpressionUtils.GetExpressions(type, body);
    }

    public static void AddExpressions(IEnumerable<Expression> es, Type t, QueryBuilder qb)
    {
        CompositeExpressionUtils.PrependSelectExpressions(es, t, qb);
    }
}
