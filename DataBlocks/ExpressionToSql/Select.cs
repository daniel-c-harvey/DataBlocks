using System.Reflection;

namespace ExpressionToSql
{
    using ScheMigrator.Migrations;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public class Select<T, R> : Query
    {
        private readonly Expression<Func<T, R>> _select;
        private readonly int? _take;
        private readonly Table _table;

        internal Select(Expression<Func<T, R>> select, int? take, Table table, ISqlDialect dialect) 
            : base(dialect)
        {
            _select = select;
            _take = take;
            _table = table;
        }

        public Where<T, R> Where(Expression<Func<T, bool>> predicate)
        {
            return new Where<T, R>(this, predicate);
        }

        public Limit<T, R> Limit(int count)
        {
            return new Limit<T, R>(this, count);
        }

        public Page<T, R> Page(int pageNumber, int pageSize)
        {
            return new Page<T, R>(this, pageNumber, pageSize);
        }

        /// <summary>
        /// Adds an OFFSET clause to the query for paging support
        /// </summary>
        /// <param name="offset">The number of rows to skip</param>
        /// <returns>An Offset query object</returns>
        public Offset<T, R> Offset(int offset)
        {
            return new Offset<T, R>(this, offset);
        }

        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            if (_take.HasValue)
            {
                qb.Take(_take.Value);
            }

            var type = _select.Parameters[0].Type;

            var expressions = GetExpressions(type, _select.Body);

            AddExpressions(expressions, type, qb);

            qb.AddTable(_table);

            return qb;
        }

        private static IEnumerable<Expression> GetExpressions(Type type, Expression body)
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

        private static void AddExpressions(IEnumerable<Expression> es, Type t, QueryBuilder qb)
        {
            foreach (var e in es)
            {
                AddExpression(e, t, qb);
                qb.AddSeparator();
            }
            qb.Remove(); // Remove last comma
        }

        private static void AddExpression(Expression e, Type t, QueryBuilder qb)
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

        private static void AddExpression(MemberExpression m, Type t, QueryBuilder qb)
        {
            if (m.Member.DeclaringType.IsAssignableFrom(t))
            {
                var schemaDataAttr = m.Member.GetCustomAttributes(typeof(ScheDataAttribute), true).FirstOrDefault() as ScheDataAttribute;
                if (schemaDataAttr != null && !string.IsNullOrEmpty(schemaDataAttr.FieldName))
                {
                    qb.AddAttribute(schemaDataAttr.FieldName, m.Member.Name);
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

    public class Select<T1, T2, R>
    {
        internal Select(Expression<Func<T1, T2, R>> select, Expression<Func<T1, T2, bool>> on, int? take, Table table, ISqlDialect dialect)
        {
        }

        public override string ToString()
        {
            return "";
        }
    }
}