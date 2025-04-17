namespace ExpressionToSql
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Manages mappings between types and aliases in SQL queries.
    /// </summary>
    public class AliasRegistry
    {
        // Core mappings
        private readonly Dictionary<Type, string> _typeToAlias = new Dictionary<Type, string>();
        private readonly Dictionary<string, Type> _aliasToType = new Dictionary<string, Type>();
        
        // Alias redirections (e.g., for subqueries)
        private readonly Dictionary<string, string> _aliasRedirects = new Dictionary<string, string>();
        
        // Parameter to alias mappings
        private readonly Dictionary<string, string> _parameterAliases = new Dictionary<string, string>();
        
        /// <summary>
        /// Registers a type with a specific alias.
        /// </summary>
        public string RegisterType(Type type, string alias = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "Type cannot be null when registering an alias");
                
            // If no alias provided, use existing or create new one
            if (string.IsNullOrEmpty(alias))
            {
                if (_typeToAlias.TryGetValue(type, out alias))
                    return alias;
                    
                alias = GenerateAliasForType(type);
            }
            
            // Register bidirectional mapping
            _typeToAlias[type] = alias;
            _aliasToType[alias] = type;
            
            return alias;
        }
        
        /// <summary>
        /// Registers a parameter with an alias.
        /// </summary>
        public void RegisterParameterAlias(string paramName, Type paramType, string alias)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentException("Parameter name cannot be null or empty", nameof(paramName));
                
            if (paramType == null)
                throw new ArgumentNullException(nameof(paramType), "Parameter type cannot be null");
                
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentException("Alias cannot be null or empty", nameof(alias));
                
            // Store parameter alias mapping
            _parameterAliases[paramName] = alias;
            
            // Also ensure type mapping exists
            RegisterType(paramType, alias);
        }
        
        /// <summary>
        /// Redirects one alias to another (for subqueries).
        /// </summary>
        public void RedirectAlias(string sourceAlias, string targetAlias)
        {
            if (string.IsNullOrEmpty(sourceAlias))
                throw new ArgumentException("Source alias cannot be null or empty", nameof(sourceAlias));
                
            if (string.IsNullOrEmpty(targetAlias))
                throw new ArgumentException("Target alias cannot be null or empty", nameof(targetAlias));
                
            _aliasRedirects[sourceAlias] = targetAlias;
        }
        
        /// <summary>
        /// Gets the alias for a type.
        /// </summary>
        public string GetAliasForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "Type cannot be null when getting an alias");
                
            if (!_typeToAlias.TryGetValue(type, out var alias))
                throw new InvalidOperationException($"No alias mapping found for type {type.Name}");
                
            return GetEffectiveAlias(alias);
        }
        
        /// <summary>
        /// Gets the type for an alias.
        /// </summary>
        public Type GetTypeForAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentException("Alias cannot be null or empty", nameof(alias));
                
            alias = GetEffectiveAlias(alias);
            
            if (!_aliasToType.TryGetValue(alias, out var type))
                throw new InvalidOperationException($"No type mapping found for alias '{alias}'");
                
            return type;
        }
        
        /// <summary>
        /// Gets the effective alias after any redirections.
        /// </summary>
        public string GetEffectiveAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentException("Alias cannot be null or empty", nameof(alias));
                
            string current = alias;
            HashSet<string> visited = new HashSet<string>();
            
            while (_aliasRedirects.TryGetValue(current, out var redirected) && !visited.Contains(current))
            {
                visited.Add(current);
                current = redirected;
            }
            
            return current;
        }
        
        /// <summary>
        /// Resolves the appropriate alias for a member expression.
        /// </summary>
        public string GetAliasForMemberExpression(MemberExpression member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member), "Member expression cannot be null");
                
            // 1. If it's a parameter reference, check parameter aliases
            if (member.Expression is ParameterExpression paramExpr)
            {
                if (_parameterAliases.TryGetValue(paramExpr.Name, out var alias))
                    return GetEffectiveAlias(alias);
                    
                // Try the parameter's type
                return GetAliasForType(paramExpr.Type);
            }
            else if (member.Expression is UnaryExpression  unaryExpr)
            {
                return GetAliasForType(unaryExpr.Operand.Type);
            }
            
            // 2. Try the member's declaring type
            var declaringType = member.Member.DeclaringType;
            if (declaringType != null)
            {
                return GetAliasForType(declaringType);
            }
            
            throw new InvalidOperationException($"Could not resolve alias for member expression {member}");
        }
        
        /// <summary>
        /// Determines if an expression is part of the query context.
        /// </summary>
        public bool IsExpressionInQueryContext(Expression expr)
        {
            if (expr == null)
                return false;
                
            // Parameter expression is in context if we have its alias
            if (expr is ParameterExpression paramExpr)
                return _parameterAliases.ContainsKey(paramExpr.Name) || HasAliasForType(paramExpr.Type);
                
            // Member expression needs more careful analysis
            if (expr is MemberExpression memberExpr)
            {
                // Closure variable (constant) is never in context
                if (memberExpr.Expression is ConstantExpression)
                    return false;
                    
                // Parameter-based members are in context if parameter is registered
                if (memberExpr.Expression is ParameterExpression parameterExpr)
                    return _parameterAliases.ContainsKey(parameterExpr.Name) || HasAliasForType(parameterExpr.Type);
                    
                // Check if declaring type is registered
                if (memberExpr.Member.DeclaringType != null && HasAliasForType(memberExpr.Member.DeclaringType))
                    return true;
                    
                // Recursively check the containing expression
                return IsExpressionInQueryContext(memberExpr.Expression);
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the dominant alias for a binary expression.
        /// </summary>
        public string GetDominantAliasForBinaryExpression(BinaryExpression expr)
        {
            if (expr == null)
                throw new ArgumentNullException(nameof(expr), "Binary expression cannot be null");
                
            // For comparison operations, check both sides
            if (IsBinaryComparison(expr))
            {
                // Left side is usually the dominant one
                if (expr.Left is MemberExpression leftMember)
                    return GetAliasForMemberExpression(leftMember);
                    
                // If left is constant and right is member, use right
                if (expr.Left.NodeType == ExpressionType.Constant && 
                    expr.Right is MemberExpression rightMember)
                    return GetAliasForMemberExpression(rightMember);
            }
            
            throw new InvalidOperationException($"Could not determine dominant alias for binary expression {expr}");
        }
        
        /// <summary>
        /// Checks if a type has a registered alias.
        /// </summary>
        public bool HasAliasForType(Type type)
        {
            return type != null && _typeToAlias.ContainsKey(type);
        }
        
        /// <summary>
        /// Gets all type-to-alias mappings.
        /// </summary>
        public IEnumerable<KeyValuePair<Type, string>> GetTypeMappings()
        {
            return _typeToAlias.ToList();
        }
        
        /// <summary>
        /// Copies all mappings from another AliasRegistry.
        /// </summary>
        public void CopyFrom(AliasRegistry source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Source cannot be null");
                
            // Copy all type mappings
            foreach (var pair in source._typeToAlias)
                _typeToAlias[pair.Key] = pair.Value;
                
            foreach (var pair in source._aliasToType)
                _aliasToType[pair.Key] = pair.Value;
                
            // Copy redirections
            foreach (var pair in source._aliasRedirects)
                _aliasRedirects[pair.Key] = pair.Value;
                
            // Copy parameter aliases
            foreach (var pair in source._parameterAliases)
                _parameterAliases[pair.Key] = pair.Value;
        }
        
        // Helper to generate an alias for a type
        private string GenerateAliasForType(Type type)
        {
            string baseAlias = char.ToLowerInvariant(type.Name[0]).ToString();
            
            // Make sure it's unique
            if (!_aliasToType.ContainsKey(baseAlias))
                return baseAlias;
                
            // Add numeric suffix until unique
            int counter = 1;
            string candidate;
            
            do
            {
                candidate = $"{baseAlias}{counter++}";
            }
            while (_aliasToType.ContainsKey(candidate));
            
            return candidate;
        }
        
        // Helper to check if an expression is a binary comparison
        private bool IsBinaryComparison(BinaryExpression expr)
        {
            return expr.NodeType == ExpressionType.Equal ||
                   expr.NodeType == ExpressionType.NotEqual ||
                   expr.NodeType == ExpressionType.GreaterThan ||
                   expr.NodeType == ExpressionType.GreaterThanOrEqual ||
                   expr.NodeType == ExpressionType.LessThan ||
                   expr.NodeType == ExpressionType.LessThanOrEqual;
        }
    }
} 