using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DataBlocks.ExpressionToSql.Expressions;
using ExpressionToSql.Utils;
using ScheMigrator.Migrations;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Utility methods for working with composite expression trees
    /// </summary>
    internal static class CompositeExpressionUtils
    {
        /// <summary>
        /// Gets the expressions from a given body
        /// </summary>
        public static IEnumerable<Expression> GetExpressions(Type type, Expression body)
        {
            switch (body.NodeType)
            {
                case ExpressionType.New:
                    var n = (NewExpression)body;
                    return n.Arguments;
                case ExpressionType.Parameter:
                    var propertyInfos = type.GetProperties().Where(p => 
                        p.GetCustomAttributes(typeof(ScheDataAttribute), true).Any());
                    return propertyInfos.Select(pi => Expression.Property(body, pi));
                default:
                    return new[] { body };
            }
        }

        /// <summary>
        /// Adds expressions to the query builder
        /// </summary>
        public static void AddExpressions(IEnumerable<Expression> es, Type t, QueryBuilder qb)
        {
            foreach (var e in es)
            {
                AddExpression(e, t, qb);
                qb.AddSeparator();
            }
            qb.Remove(); // Remove last comma
        }

        /// <summary>
        /// Adds a single expression to the query builder
        /// </summary>
        private static void AddExpression(Expression e, Type t, QueryBuilder qb)
        {
            switch (e.NodeType)
            {
                case ExpressionType.Constant:
                    var c = (ConstantExpression)e;
                    qb.AddValue(c.Value);
                    break;
                case ExpressionType.MemberAccess:
                    var m = (MemberExpression)e;
                    AddExpression(m, t, qb);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Adds a member expression to the query builder
        /// </summary>
        private static void AddExpression(MemberExpression m, Type t, QueryBuilder qb)
        {
            if (m.Member.DeclaringType.IsAssignableFrom(t))
            {
                try
                {
                    // Use the centralized utility function to get the field name
                    string fieldName = SqlTypeUtils.ResolveFieldName(m.Member, t);
                    if (fieldName != m.Member.Name)
                    {
                        qb.AddAttribute(fieldName, m.Member.Name);
                        return;
                    }
                }
                catch (Exception)
                {
                    // If the field name resolution fails, fall back to member name
                }
                qb.AddAttribute(m.Member.Name);
            }
            else
            {
                qb.AddParameter(m.Member.Name);
            }
        }
    }
} 