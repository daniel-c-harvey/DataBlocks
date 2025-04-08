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
        
        // Track query parameters by name and type for better instance identification
        private HashSet<string> _queryParameters = new HashSet<string>();

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
        
        // Register type-to-alias mapping with direct Type object
        public void RegisterTableAliasForType(Type type, string alias)
        {
            _tableAliases[type] = alias;
        }

        // Get alias for a type
        public string? GetAliasForType(Type type)
        {
            if (type == null)
                return null;
        
            return _tableAliases.TryGetValue(type, out var alias) ? alias : null;
        }

        // Check if a type has a registered alias
        public bool HasAliasForType(Type type)
        {
            if (type == null)
                return false;
        
            return _tableAliases.ContainsKey(type);
        }

        // Get alias or create a new one if it doesn't exist
        public string GetOrCreateAliasForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "Type cannot be null when getting or creating an alias");
        
            // Check if the type already has an alias
            if (_tableAliases.TryGetValue(type, out var existingAlias))
            {
                return existingAlias;
            }
        
            // Create a new alias
            var newAlias = GetNextAlias();
            RegisterTableAliasForType(type, newAlias);
            return newAlias;
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

        // Register a parameter as part of the current query
        public void RegisterQueryParameter(string paramName, Type paramType)
        {
            if (string.IsNullOrEmpty(paramName))
                return;
            
            // Store a composite key of name+type to uniquely identify parameters
            string key = $"{paramName}_{paramType.FullName}";
            _queryParameters.Add(key);
        }
        
        // Check if a parameter is part of the current query context
        public bool IsParameterInQuery(string paramName, Type paramType)
        {
            if (string.IsNullOrEmpty(paramName))
                return false;
            
            // Create composite key from name and type
            string key = $"{paramName}_{paramType.FullName}";
            return _queryParameters.Contains(key);
        }
    }
}