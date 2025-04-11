using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ExpressionToSql;
using ExpressionToSql.Composite;

namespace DataBlocks.ExpressionToSql.Expressions
{
    /// <summary>
    /// Extension methods for building SQL expressions in a fluent way
    /// </summary>
    public static class ExpressionBuilderExtensions
    {
        /// <summary>
        /// Adds SELECT expressions to the query builder by prepending them at the beginning
        /// </summary>
        public static QueryBuilder Select<T>(this QueryBuilder queryBuilder, Expression<Func<T, object>> selector)
        {
            var expressions = CompositeExpressionUtils.GetExpressions(typeof(T), selector.Body);
            CompositeExpressionUtils.PrependSelectExpressions(expressions, typeof(T), queryBuilder);
            return queryBuilder;
        }

        /// <summary>
        /// Adds expressions to the query builder (appends them at the end)
        /// </summary>
        public static QueryBuilder AppendExpressions<T>(this QueryBuilder queryBuilder, Expression<Func<T, object>> selector)
        {
            var expressions = CompositeExpressionUtils.GetExpressions(typeof(T), selector.Body);
            CompositeExpressionUtils.AddExpressions(expressions, typeof(T), queryBuilder);
            return queryBuilder;
        }

        /// <summary>
        /// Adds expressions to the query builder with explicit prepend/append control
        /// </summary>
        public static QueryBuilder AddExpressions<T>(this QueryBuilder queryBuilder, Expression<Func<T, object>> selector, bool prepend = false)
        {
            var expressions = CompositeExpressionUtils.GetExpressions(typeof(T), selector.Body);
            CompositeExpressionUtils.AddExpressions(expressions, typeof(T), queryBuilder, prepend);
            return queryBuilder;
        }

        /// <summary>
        /// Prepends expressions to the query builder (adds them at the beginning)
        /// </summary>
        public static QueryBuilder PrependExpressions<T>(this QueryBuilder queryBuilder, Expression<Func<T, object>> selector)
        {
            var expressions = CompositeExpressionUtils.GetExpressions(typeof(T), selector.Body);
            CompositeExpressionUtils.AddExpressions(expressions, typeof(T), queryBuilder, true);
            return queryBuilder;
        }

        /// <summary>
        /// Adds a composite expression as a SELECT statement by prepending it at the beginning
        /// </summary>
        public static QueryBuilder SelectComposite<T, TJoin>(this QueryBuilder queryBuilder, Expression<Func<T, object>> selector, params Type[] additionalJoinTypes)
        {
            var joinTypes = new List<Type> { typeof(TJoin) };
            if (additionalJoinTypes != null && additionalJoinTypes.Length > 0)
            {
                joinTypes.AddRange(additionalJoinTypes);
            }
            
            var expressions = CompositeExpressionUtils.GetExpressions(typeof(T), selector.Body);
            CompositeExpressionUtils.PrependSelectExpressions(expressions, typeof(T), queryBuilder, joinTypes.ToArray());
            return queryBuilder;
        }

        /// <summary>
        /// Adds a composite expression without any join types as a SELECT statement
        /// </summary>
        public static QueryBuilder SelectComposite<T>(this QueryBuilder queryBuilder, Expression<Func<T, object>> selector)
        {
            var expressions = CompositeExpressionUtils.GetExpressions(typeof(T), selector.Body);
            CompositeExpressionUtils.PrependSelectExpressions(expressions, typeof(T), queryBuilder);
            return queryBuilder;
        }
    }
} 