using System;
using System.Linq.Expressions;
using ExpressionToSql;
using ExpressionToSql.Utils;
using ScheMigrator.Migrations;

namespace DataBlocks.ExpressionToSql.Expressions;

internal static class SelectExpressions
{
    public static IEnumerable<Expression> GetExpressions(Type type, Expression body)
    {
        switch (body.NodeType)
        {
            case ExpressionType.New:
                var n = (NewExpression) body;
                return n.Arguments;
            case ExpressionType.Parameter:
                var propertyInfos = type.GetProperties().Where(p => p.GetCustomAttributes(typeof(ScheDataAttribute), true).Any());
                return propertyInfos.Select(pi => Expression.Property(body, pi));
            default:
                return new[] { body };
        }
    }

    public static void AddExpressions(IEnumerable<Expression> es, Type t, QueryBuilder qb)
    {
        foreach (var e in es)
        {
            AddExpression(e, t, qb);
            qb.AddSeparator();
        }
        qb.Remove(); // Remove last comma
    }

    public static void AddExpression(Expression e, Type t, QueryBuilder qb)
    {
        switch (e.NodeType)
        {
            case ExpressionType.Constant:
                var c = (ConstantExpression) e;
                qb.AddValue(c.Value);
                break;
            case ExpressionType.MemberAccess:
                var m = (MemberExpression) e;
                AddExpression(m, t, qb);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public static void AddExpression(MemberExpression m, Type t, QueryBuilder qb)
    {
        if (m.Member.DeclaringType.IsAssignableFrom(t))
        {
            // Use the centralized utility function to get the field name
            string fieldName = SqlTypeUtils.ResolveFieldName(m.Member, t);
            if (fieldName != m.Member.Name)
            {
                qb.AddAttribute(fieldName, m.Member.Name);
                return;
            }
            qb.AddAttribute(m.Member.Name);
        }
        else
        {
            qb.AddParameter(m.Member.Name);
        }
    }
}
