namespace ExpressionToSql
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;

    public abstract class Query
    {
        protected internal ISqlDialect Dialect { get; private set; }
        protected internal Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        
        // Replace EntityTypes dictionary with AliasRegistry
        protected internal AliasRegistry Aliases { get; } = new AliasRegistry();
        
        // Keep EntityTypes for backward compatibility
        // This will be used for transitioning to AliasRegistry
        protected internal Dictionary<string, Type> EntityTypes { get; } = new Dictionary<string, Type>();
        
        // Dictionary to track expression parameters
        protected internal Dictionary<string, (string Name, Type Type)> ExpressionParameters { get; } = new Dictionary<string, (string, Type)>();

        protected Query(ISqlDialect dialect)
        {
            Dialect = dialect;
        }
        
        // Method to register an entity type for an alias (updated to use AliasRegistry)
        protected internal void RegisterEntityType(string alias, Type entityType)
        {
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentException("Alias cannot be null or empty", nameof(alias));
                
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType), "Entity type cannot be null");
                
            // Register in AliasRegistry
            Aliases.RegisterType(entityType, alias);
            
            // Keep for backward compatibility
            EntityTypes[alias] = entityType;
        }
        
        // Method to retrieve the entity type for an alias (updated to use AliasRegistry)
        protected internal Type GetEntityType(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentException("Alias cannot be null or empty", nameof(alias));
                
            return Aliases.GetTypeForAlias(alias);
        }
        
        // New method to get alias for type using AliasRegistry
        protected internal string GetAliasForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "Type cannot be null");
                
            return Aliases.GetAliasForType(type);
        }
        
        // Method to copy alias mappings from another query
        protected internal void CopyAliasesFrom(Query sourceQuery)
        {
            if (sourceQuery == null)
                return;
                
            // Copy from AliasRegistry
            Aliases.CopyFrom(sourceQuery.Aliases);
            
            // Also copy EntityTypes for backward compatibility
            foreach (var pair in sourceQuery.EntityTypes)
            {
                EntityTypes[pair.Key] = pair.Value;
            }
            
            // Also copy parameter registrations
            foreach (var pair in sourceQuery.ExpressionParameters)
            {
                ExpressionParameters[pair.Key] = pair.Value;
            }
        }
        
        // For backward compatibility, retain CopyEntityTypesFrom but implement using CopyAliasesFrom
        protected internal void CopyEntityTypesFrom(Query sourceQuery)
        {
            CopyAliasesFrom(sourceQuery);
        }
        
        protected internal void CopyParametersFromType(Query baseQuery)
        {
            if (baseQuery == null)
                return;
                
            foreach (var (key, value) in baseQuery.Parameters)
            {
                Parameters.TryAdd(key, value);
            }
        }
        
        // Enhanced method to apply aliases to a QueryBuilder
        protected internal void ApplyAliasesToQueryBuilder(QueryBuilder qb)
        {
            if (qb == null)
                return;
                
            // Register all type mappings in the QueryBuilder
            foreach (var mapping in Aliases.GetTypeMappings())
            {
                Type type = mapping.Key;
                string alias = mapping.Value;
                
                qb.RegisterTableAliasForType(type, alias);
            }
        }
        
        // For backward compatibility, rename but keep the functionality
        protected internal void ApplyEntityTypesToQueryBuilder(QueryBuilder qb)
        {
            ApplyAliasesToQueryBuilder(qb);
        }
        
        // Method to register parameters from a lambda expression
        protected internal void RegisterExpressionParameter<TDelegate>(Expression<TDelegate> expression)
        {
            if (expression == null) return;
            
            foreach (var param in expression.Parameters)
            {
                // Register parameter by name and type for instance tracking
                RegisterParameter(param.Name, param.Type);
            }
        }
        
        // More direct method to register a parameter
        protected internal void RegisterParameter(string paramName, Type paramType)
        {
            if (string.IsNullOrEmpty(paramName) || paramType == null) return;
            
            // Create a unique key for this parameter
            string key = $"{paramName}_{paramType.FullName}";
            
            // Register the parameter for later lookup
            ExpressionParameters[key] = (paramName, paramType);
        }
        
        // Method to check if a parameter exists in the query context
        protected internal bool HasExpressionParameter(string paramName, Type paramType)
        {
            if (string.IsNullOrEmpty(paramName)) return false;
            
            string key = $"{paramName}_{paramType.FullName}";
            return ExpressionParameters.ContainsKey(key);
        }

        public override string ToString()
        {
            return ToSql();
        }
        
        public string ToSql()
        {
            return ToSql(new StringBuilder()).ToString();
        }

        public StringBuilder ToSql(StringBuilder sb)
        {
            ToSql(new QueryBuilder(sb, Dialect, this));
            return sb;
        }

        internal abstract QueryBuilder ToSql(QueryBuilder qb);
    }

    public abstract class QueryRoot<TRoot> : Query
    {
        protected QueryRoot(ISqlDialect dialect) : base(dialect) { }
    }
}