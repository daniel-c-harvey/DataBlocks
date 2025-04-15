using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ScheMigrator.Migrations;

namespace ExpressionToSql.Utils
{
    /// <summary>
    /// Utility functions for SQL type handling and name resolution
    /// </summary>
    public static class SqlTypeUtils
    {
        /// <summary>
        /// Resolves the field name from a member, accounting for ScheDataAttribute if present
        /// </summary>
        /// <param name="memberInfo">The member info to resolve</param>
        /// <param name="entityType">The entity type that contains this member</param>
        /// <returns>The field name to use in SQL queries</returns>
        public static string ResolveFieldName(MemberInfo memberInfo, Type entityType)
        {
            if (entityType != null && memberInfo.DeclaringType != null && 
                memberInfo.DeclaringType.IsAssignableFrom(entityType))
            {
                var schemaDataAttr = memberInfo.GetCustomAttributes(typeof(ScheDataAttribute), true)
                    .FirstOrDefault() as ScheDataAttribute;
                    
                if (schemaDataAttr != null && !string.IsNullOrEmpty(schemaDataAttr.FieldName))
                {
                    return schemaDataAttr.FieldName;
                }
                else
                {
                    var property = entityType.GetProperty(memberInfo.Name);
                    if (property != null)
                    {
                        var actualAttributes = property
                            .GetCustomAttributes(true)
                            .Where(a => a is ScheDataAttribute)
                            .FirstOrDefault() as ScheDataAttribute;
                            
                        if (actualAttributes != null && !string.IsNullOrEmpty(actualAttributes.FieldName))
                        {
                            return actualAttributes.FieldName;
                        }
                    }
                    
                }
            }
            throw new Exception($"ScheData attribute not found for member {memberInfo.Name} on type {entityType.FullName}");
        }

        public static IEnumerable<string> GetColumnNames(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Get all public properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            // Use ResolveFieldName which already handles ScheDataAttribute lookup
            return properties
                .Select(p => 
                {
                    try 
                    {
                        // If ResolveFieldName succeeds, the property has ScheDataAttribute
                        return ResolveFieldName(p, type);
                    }
                    catch (Exception)
                    {
                        // If ResolveFieldName throws, the property doesn't have ScheDataAttribute
                        return string.Empty;
                    }
                })
                .Where(p => !string.IsNullOrEmpty(p));
        }
    }
} 