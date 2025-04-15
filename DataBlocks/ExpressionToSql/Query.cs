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
        
        // Dictionary to track entity types by alias
        protected internal Dictionary<string, Type> EntityTypes { get; } = new Dictionary<string, Type>();
        
        // Dictionary to track expression parameters
        protected internal Dictionary<string, (string Name, Type Type)> ExpressionParameters { get; } = new Dictionary<string, (string, Type)>();

        protected Query(ISqlDialect dialect)
        {
            Dialect = dialect;
        }
        
        // Method to register an entity type for an alias
        protected internal void RegisterEntityType(string alias, Type entityType)
        {
            EntityTypes[alias] = entityType;
        }
        
        // Method to retrieve the entity type for an alias
        protected internal Type GetEntityType(string alias)
        {
            return EntityTypes.TryGetValue(alias, out var type) ? type : null;
        }
        
        // Method to lookup an alias for a given type - centralizing alias resolution logic
        protected internal string GetAliasForType(Type type)
        {
            if (type == null) return null;
            
            // Look through all mappings for this type
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    return pair.Key;
                }
            }
            
            return null;
        }
        
        // Method to copy entity types from another query
        protected internal void CopyEntityTypesFrom(Query sourceQuery)
        {
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
        
        protected internal void CopyParametersFromType(Query baseQuery)
        {
            foreach (var (key, value) in baseQuery.Parameters)
            {
                Parameters.TryAdd(key, value);
            }
        }
        
        // Method to ensure all types have appropriate aliases registered
        protected internal void EnsureTypesHaveAliases(QueryBuilder qb)
        {
            // First register all entity types we know about
            foreach (var pair in EntityTypes)
            {
                // Register type-to-alias mapping
                qb.RegisterTableAliasForType(pair.Value, pair.Key);
            }
            
            // Then scan for types that are used in the query but don't have aliases
            var typesWithoutAliases = new HashSet<Type>();
            
            // Check all types in expression parameters
            foreach (var pair in ExpressionParameters.Values)
            {
                Type paramType = pair.Type;
                if (!qb.HasAliasForType(paramType) && !typesWithoutAliases.Contains(paramType))
                {
                    typesWithoutAliases.Add(paramType);
                }
            }
            
            // For each type without an alias, look for existing registrations
            foreach (var type in typesWithoutAliases)
            {
                // Look for the type in our entity mappings
                string existingAlias = null;
                foreach (var pair in EntityTypes)
                {
                    if (pair.Value == type)
                    {
                        existingAlias = pair.Key;
                        break;
                    }
                }
                
                if (!string.IsNullOrEmpty(existingAlias))
                {
                    // Register with the existing alias
                    qb.RegisterTableAliasForType(type, existingAlias);
                }
                else
                {
                    // Create a new alias if needed
                    string newAlias = qb.GetOrCreateAliasForType(type);
                    
                    // Register the new type-alias mapping
                    RegisterEntityType(newAlias, type);
                }
            }
        }
        
        // Helper to ensure alias mappings are properly applied to a QueryBuilder
        protected internal void ApplyEntityTypesToQueryBuilder(QueryBuilder qb)
        {
            // First register entity types directly
            foreach (var pair in EntityTypes)
            {
                // Register each entity type with its alias in the QueryBuilder
                Type entityType = pair.Value;
                string alias = pair.Key;
                
                qb.RegisterTableAliasForType(entityType, alias);
            }
            
            // Then call EnsureTypesHaveAliases to make sure nothing is missing
            EnsureTypesHaveAliases(qb);
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