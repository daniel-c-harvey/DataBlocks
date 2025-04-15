using System.Linq.Expressions;

namespace ExpressionToSql
{
    using System.Text;
    using System.Collections.Generic;
    using System;
    using ExpressionToSql.Utils;

    /// <summary>
    /// Tracks the state of the query builder to know how to format SQL
    /// </summary>
    public enum QueryBuilderState
    {
        /// <summary>
        /// Initial state, no SELECT has been added
        /// </summary>
        Initial,
        
        /// <summary>
        /// A subquery has been added, no SELECT has been prepended
        /// </summary>
        Subquery,
        
        /// <summary>
        /// A SELECT has been prepended
        /// </summary>
        SelectPrepended
    }

    public class QueryBuilder
    {
        private readonly StringBuilder _sb;
        private readonly ISqlDialect _dialect;
        public readonly Query _query;
        private bool _firstCondition = true;
        public const string TableAliasName = "a";
        
        // Track the state of the query builder
        private QueryBuilderState _state = QueryBuilderState.Initial;

        // Track current aliases
        private Dictionary<Type, string> _tableAliases = new Dictionary<Type, string>();
        
        // Track explicit alias mappings (e.g., "a" → "subq")
        private Dictionary<string, string> _aliasMappings = new Dictionary<string, string>();

        // Track query parameters by name and type for better instance identification
        private HashSet<string> _queryParameters = new HashSet<string>();

        public QueryBuilder(StringBuilder sb, ISqlDialect dialect, Query query)
        {
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

        public QueryBuilder AddParameter(string parameterName, bool prepend = false)
        {
            var tempSB = new StringBuilder();
            tempSB.Append(" ").Append(_dialect.FormatParameter(parameterName));
            if (prepend)
                _sb.Insert(0, tempSB);
            else
                _sb.Append(tempSB);
            
            return this;
        }

        public QueryBuilder AddParameterWithValue(string name, object value)
        {
            // Track the parameter in our owner query if we have one
            if (_query != null && value != null)
            {
                _query.Parameters[name] = value;
            }
            
            // Add the parameter reference to the SQL
            _sb.Append(" @").Append(name);
            
            return this;
        }

        public QueryBuilder AddAttribute(string attributeName, string columnAliasName = "", string tableAliasName = TableAliasName, bool prepend = false)
        {
            var tempSB = new StringBuilder();
            tempSB.Append(" ");

            if (!string.IsNullOrWhiteSpace(tableAliasName))
            {
                tempSB.Append(tableAliasName).Append(".");
            }

            tempSB.Append(_dialect.EscapeIdentifier(attributeName));

            if (!string.IsNullOrWhiteSpace(columnAliasName))
            {
                tempSB.Append(" AS ").Append(columnAliasName);
            }
            
            if (prepend)
                _sb.Insert(0, tempSB.ToString());
            else
                _sb.Append(tempSB.ToString());
            
            return this;
        }

        public QueryBuilder AddValue(object value, bool prepend = false)
        {
            if (prepend)
                _sb.Insert(0, value).Insert(0, " "); 
            else
                _sb.Append(" ").Append(value);
            return this;
        }
        
        // public QueryBuilder AddConstant(object value)
        // {
        //     _sb.Append(" ").Append(value);
        //     return this;
        // }

        public QueryBuilder AddSeparator(bool prepend = false)
        {
            if (prepend)
            {
                // When prepending, insert the comma at index 0
                _sb.Insert(0, ",");
            }
            else
            {
                // When appending, just add the comma at the end
                _sb.Append(',');
            }
            return this;
        }

        // Additional separator handling methods for clarity
        public QueryBuilder PrependSeparator()
        {
            _sb.Insert(0, ",");
            return this;
        }

        public QueryBuilder AppendSeparator()
        {
            _sb.Append(',');
            return this;
        }

        public QueryBuilder RemoveLastSeparator()
        {
            if (_sb.Length > 0 && _sb[_sb.Length - 1] == ',')
            {
                _sb.Length -= 1;
            }
            return this;
        }

        public QueryBuilder RemoveFirstSeparator()
        {
            if (_sb.Length > 0 && _sb[0] == ',')
            {
                _sb.Remove(0, 1);
            }
            return this;
        }

        // Add method to insert text at a specific position
        public QueryBuilder InsertText(int position, string text)
        {
            if (position >= 0 && position <= _sb.Length)
            {
                _sb.Insert(position, text);
            }
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

        public QueryBuilder PrependText(string text)
        {
            _sb.Insert(0, text);
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
            if (type == null || string.IsNullOrEmpty(alias))
                return;
            
            _tableAliases[type] = alias;
            
            // Also check if this alias is mapped to another alias
            if (_aliasMappings.TryGetValue(alias, out var mappedAlias))
            {
                // Register the type with the mapped alias too
                _tableAliases[type] = mappedAlias;
            }
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

        /// <summary>
        /// Adds a subquery to the FROM clause with the given alias
        /// </summary>
        /// <param name="subquerySql">The SQL text of the subquery</param>
        /// <param name="alias">The alias for the subquery</param>
        /// <returns>The updated QueryBuilder</returns>
        public QueryBuilder AddSubquery(string subquerySql, string alias)
        {
            _sb.Append(" FROM (");
            
            // Ensure the subquery starts with SELECT if it doesn't already
            if (!subquerySql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                _sb.Append("SELECT ");
            }
            
            _sb.Append(subquerySql);
            _sb.Append(")");

            if (!string.IsNullOrWhiteSpace(alias))
            {
                _sb.Append(" AS ").Append(alias);
            }
            
            // Update state to indicate a subquery has been added
            _state = QueryBuilderState.Subquery;
            
            return this;
        }
        
        /// <summary>
        /// Prepends a SELECT statement to the query
        /// </summary>
        public QueryBuilder PrependSelect()
        {
            if (_state == QueryBuilderState.SelectPrepended)
            {
                // SELECT has already been prepended, no need to do it again
                return this;
            }

            // Ensure proper spacing between SELECT and the rest of the query
            string currentQuery = _sb.ToString().TrimStart();
            _sb.Clear();
            _sb.Append("SELECT ");
            _sb.Append(currentQuery);
            
            // Update state to indicate SELECT has been prepended
            _state = QueryBuilderState.SelectPrepended;
            
            return this;
        }

        public QueryBuilder PrependSelectExpression(Expression e, Type t)
        {
            switch (e.NodeType)
            {
                case ExpressionType.Constant:
                    var c = (ConstantExpression) e;
                    AddValue(c.Value, prepend: true);
                    break;
                case ExpressionType.MemberAccess:
                    var m = (MemberExpression) e;
                    PrependExpression(m, t);
                    break;
                default:
                    throw new NotImplementedException($"Expression type {e.NodeType} not supported in PrependSelectExpression: {e}");
            }
            return this;
        }

        public void PrependExpression(MemberExpression m, Type t)
        {
            if (m.Member.DeclaringType.IsAssignableFrom(t))
            {
                // Use the centralized utility function to get the field name
                string fieldName = SqlTypeUtils.ResolveFieldName(m.Member, t);
                if (fieldName != m.Member.Name)
                {
                    AddAttribute(fieldName, m.Member.Name, prepend: true);
                    return;
                }
                AddAttribute(m.Member.Name, prepend: true);
            }
            else
            {
                AddParameter(m.Member.Name, prepend: true);
            }
        }

        public QueryBuilder ReplaceAliasInJoinCondition(string oldAlias, string newAlias)
        {
            // If the aliases are the same, no need to do anything
            if (string.IsNullOrEmpty(oldAlias) || string.IsNullOrEmpty(newAlias) || oldAlias == newAlias)
                return this;
            
            // Get the current SQL
            string sql = _sb.ToString();
            
            // Find the last occurrence of the ON clause
            int onIndex = sql.LastIndexOf(" ON", StringComparison.OrdinalIgnoreCase);
            if (onIndex == -1) return this;
            
            // Get the join condition part
            string joinCondition = sql.Substring(onIndex);
            
            // Get effective aliases - apply any mappings
            oldAlias = GetEffectiveAlias(oldAlias);
            newAlias = GetEffectiveAlias(newAlias);
            
            // Find all occurrences of oldAlias followed by a dot in the join condition
            // and replace with newAlias
            string oldPattern = $"{oldAlias}.";
            string newPattern = $"{newAlias}.";
            
            // Replace the alias in the join condition
            joinCondition = joinCondition.Replace(oldPattern, newPattern);
            
            // Update the StringBuilder
            _sb.Remove(onIndex, _sb.Length - onIndex);
            _sb.Append(joinCondition);
            
            return this;
        }

        // Apply alias mappings globally to all SQL
        public QueryBuilder ApplyGlobalAliasMappings()
        {
            if (_aliasMappings.Count == 0)
                return this;
        
            // Get the current SQL
            string sql = _sb.ToString();
            string originalSql = sql;
            
            // Apply each alias mapping
            foreach (var mapping in _aliasMappings)
            {
                if (string.IsNullOrEmpty(mapping.Key) || mapping.Key == mapping.Value)
                    continue;
            
                // Replace the alias pattern (e.g., "a." → "subq.")
                string oldPattern = $"{mapping.Key}.";
                string newPattern = $"{mapping.Value}.";
                
                // Replace all occurrences in the SQL
                sql = sql.Replace(oldPattern, newPattern);
            }
            
            // If changes were made, update the StringBuilder
            if (sql != originalSql)
            {
                _sb.Clear();
                _sb.Append(sql);
            }
            
            return this;
        }

        // Check if a WHERE condition already exists for the same column and parameter
        public bool HasDuplicateWhereCondition(string column, string parameter)
        {
            string sql = _sb.ToString();
            string whereClause = sql.Contains("WHERE ", StringComparison.OrdinalIgnoreCase) 
                ? sql.Substring(sql.IndexOf("WHERE ", StringComparison.OrdinalIgnoreCase)) 
                : "";
            
            if (string.IsNullOrEmpty(whereClause))
                return false;
            
            // Look for the exact condition pattern
            string condition = $"{column} = {parameter}";
            int firstOccurrence = whereClause.IndexOf(condition, StringComparison.OrdinalIgnoreCase);
            
            if (firstOccurrence == -1)
                return false;
            
            // Check if it occurs again after the first occurrence
            return whereClause.IndexOf(condition, firstOccurrence + condition.Length, StringComparison.OrdinalIgnoreCase) != -1;
        }

        // Store a mapping from one alias to another (useful for subqueries)
        public void StoreAliasMapping(string oldAlias, string newAlias)
        {
            if (string.IsNullOrEmpty(oldAlias) || string.IsNullOrEmpty(newAlias) || oldAlias == newAlias)
                return;
            
            _aliasMappings[oldAlias] = newAlias;
            
            // If we have a type associated with the old alias, also register it with the new alias
            foreach (var kvp in _tableAliases.ToList())
            {
                if (_tableAliases[kvp.Key] == oldAlias)
                {
                    // Register this type with the new alias as well
                    _tableAliases[kvp.Key] = newAlias;
                }
            }
        }

        // Get the effective alias - handles mappings and replacements
        public string GetEffectiveAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                return alias;
        
            // If we have a stored mapping for this alias, use it
            if (_aliasMappings.TryGetValue(alias, out var mappedAlias))
                return mappedAlias;
        
            return alias;
        }
        
        // Get all mappings for a given type
        public IEnumerable<KeyValuePair<string, string>> GetAliasMappingsForType(Type type)
        {
            if (type == null || _aliasMappings.Count == 0)
                yield break;
            
            var originalAlias = GetAliasForType(type);
            if (string.IsNullOrEmpty(originalAlias))
                yield break;
            
            foreach (var mapping in _aliasMappings)
            {
                if (mapping.Key == originalAlias)
                    yield return mapping;
            }
        }

        // Clear any existing alias mapping for a specific type
        public void ClearAliasForType(Type type)
        {
            if (type == null)
                return;
            
            if (_tableAliases.ContainsKey(type))
            {
                _tableAliases.Remove(type);
            }
        }
    }
}