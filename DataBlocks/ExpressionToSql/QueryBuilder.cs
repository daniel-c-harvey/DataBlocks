namespace ExpressionToSql
{
    using System.Text;
    using System.Collections.Generic;
    using System;
    using ExpressionToSql.Utils;

    public class QueryBuilder
    {
        private readonly StringBuilder _sb;
        private readonly ISqlDialect _dialect;
        public readonly Query _query;
        private bool _firstCondition = true;
        public const string TableAliasName = "a";

        // Track current aliases
        private Dictionary<Type, string> _tableAliases = new Dictionary<Type, string>();

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

        
        public QueryBuilder AppendInClause(string attributeNameWithAlias, string paramName)
        {
            _sb.Append($" {attributeNameWithAlias} = ANY({_dialect.FormatParameter(paramName)})");
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

        public QueryBuilder AppendCondition(string condition)
        {
            if (_firstCondition)
            {
                _firstCondition = false;
                _sb.Append(" WHERE");
            }
            else
            {
                _sb.Append(" ").Append(condition);
            }
            return this;
        }

        public QueryBuilder AppendJoin(string joinType, Table table, string aliasName)
        {
            _sb.Append(" ").Append(joinType).Append(" ");

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

        public QueryBuilder AppendJoinCondition()
        {
            _sb.Append(" ON");
            return this;
        }

        public QueryBuilder Append(string text)
        {
            _sb.Append(text);
            return this;
        }

        // Get next available alias
        public string GetNextAlias() 
        {
            int aliasCount = _tableAliases.Count;
            // Generate "a", "b", "c", etc.
            char aliasChar = (char)('a' + aliasCount);
            return aliasChar.ToString();
        }

        // Register type-to-alias mapping
        public void RegisterTableAlias<T>(string alias) 
        {
            _tableAliases[typeof(T)] = alias;
        }

        // Get alias for a type
        public string GetAliasForType(Type type)
        {
            return _tableAliases.TryGetValue(type, out var alias) ? alias : TableAliasName;
        }

        // Add a method to get the condition status
        public bool IsFirstCondition()
        {
            return _firstCondition;
        }

        // Add a method to reset condition status
        public void ResetConditionState()
        {
            _firstCondition = true;
        }
    }
}