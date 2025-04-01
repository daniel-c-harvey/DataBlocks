namespace ExpressionToSql
{
    using System.Text;

    internal class QueryBuilder
    {
        private readonly StringBuilder _sb;
        private readonly ISqlDialect _dialect;
        private readonly Query _query;
        private bool _firstCondition = true;
        public const string TableAliasName = "a";

        public QueryBuilder(StringBuilder sb, ISqlDialect dialect, Query query)
        {
            if (sb.Length == 0)
            {
                sb.Append("SELECT");
            }
            _sb = sb;
            _dialect = dialect;
            _query = query;
        }

        public override string ToString()
        {
            return _sb.ToString();
        }

        public QueryBuilder Take(int count)
        {
            _sb.Append(" ").Append(_dialect.FormatTake(count));
            return this;
        }

        public QueryBuilder Offset(int offset)
        {
            _sb.Append(" ").Append(_dialect.FormatOffset(offset));
            return this;
        }

        public QueryBuilder LimitOffset(int limit, int offset)
        {
            _sb.Append(" ").Append(_dialect.FormatLimitOffset(limit, offset));
            return this;
        }

        public QueryBuilder AddParameter(string parameterName)
        {
            _sb.Append(" ").Append(_dialect.FormatParameter(parameterName));
            return this;
        }

        public QueryBuilder AddParameterWithValue(string parameterName, object value)
        {
            _sb.Append(" ").Append(_dialect.FormatParameter(parameterName));
            _query.Parameters[parameterName] = value;
            return this;
        }

        public QueryBuilder AddAttribute(string attributeName, string columnAliasName = "", string tableAliasName = TableAliasName)
        {
            _sb.Append(" ");

            if (!string.IsNullOrWhiteSpace(tableAliasName))
            {
                _sb.Append(tableAliasName).Append(".");
            }

            _sb.Append(_dialect.EscapeIdentifier(attributeName));

            if (!string.IsNullOrWhiteSpace(columnAliasName))
            {
                _sb.Append(" AS ").Append(columnAliasName);
            }
            return this;
        }

        public QueryBuilder AddValue(object value)
        {
            _sb.Append(" ").Append(value);
            return this;
        }
        
        // public QueryBuilder AddConstant(object value)
        // {
        //     _sb.Append(" ").Append(value);
        //     return this;
        // }

        public QueryBuilder AddSeparator()
        {
            _sb.Append(",");
            return this;
        }

        public QueryBuilder Remove(int count = 1)
        {
            _sb.Length -= count;
            return this;
        }

        public QueryBuilder AddTable(Table table, string aliasName = TableAliasName)
        {
            _sb.Append(" FROM ");

            var schemaName = _dialect.FormatSchemaName(table.Schema);
            if (!string.IsNullOrWhiteSpace(schemaName))
            {
                _sb.Append(schemaName);
                _sb.Append(".");
            }

            _sb.Append(_dialect.EscapeIdentifier(table.Name));

            if (!string.IsNullOrWhiteSpace(aliasName))
            {
                _sb.Append(" AS ").Append(aliasName);
            }
            return this;
        }

        public QueryBuilder AppendNot()
        {
            AppendCondition("NOT");
            return this;
        }

        public QueryBuilder AppendOpenParenthesis()
        {
            _sb.Append(" (");
            return this;
        }

        public QueryBuilder AppendCloseParenthesis()
        {
            _sb.Append(")");
            return this;
        }

        
        public QueryBuilder AppendInClause(string attributeName, string paramName)
        {
            _sb.Append($" {attributeName} = ANY({_dialect.FormatParameter(paramName)})");
            return this;
        }

        public QueryBuilder AddCondition(string op, string attributeName, string parameterName, object value, string aliasName = TableAliasName)
        {
            AppendAndCondition(op, attributeName, aliasName);
            AddParameterWithValue(parameterName, value);
            return this;
        }

        public QueryBuilder AddCondition(string op, string attributeName, object value, string aliasName = TableAliasName)
        {
            AppendAndCondition(op, attributeName, aliasName);
            AddValue(value);
            return this;
        }

        public QueryBuilder OrCondition(string op, string attributeName, string parameterName, object value, string aliasName = TableAliasName)
        {
            AppendOrCondition(op, attributeName, aliasName);
            AddParameterWithValue(parameterName, value);
            return this;
        }

        public QueryBuilder OrCondition(string op, string attributeName, object value, string aliasName = TableAliasName)
        {
            AppendOrCondition(op, attributeName, aliasName);
            AddValue(value);
            return this;
        }


        private void AppendAndCondition(string op, string attributeName, string aliasName)
        {
            AppendCondition(op, attributeName, aliasName, "AND");
        }

        private void AppendOrCondition(string op, string attributeName, string aliasName)
        {
            AppendCondition(op, attributeName, aliasName, "OR");
        }

        private void AppendCondition(string op, string attributeName, string tableAliasName, string condition)
        {
            AppendCondition(condition);
            AddAttribute(attributeName, "", tableAliasName);
            _sb.Append(" ").Append(op);
        }

        private void AppendCondition(string condition)
        {
            if (_firstCondition)
            {
                _firstCondition = false;
                condition = "WHERE";
            }
            _sb.Append(" ").Append(condition);
        }
    }
}