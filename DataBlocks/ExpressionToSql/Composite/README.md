# Composite Query Functionality

This directory contains classes that provide support for composite, strongly-typed SQL queries with multiple joins.

## Overview

The Composite Query functionality extends the existing ExpressionToSql framework to support strongly-typed queries that join multiple tables while maintaining type safety throughout the query chain. This allows for complex queries to be built in a fluent manner with full IntelliSense support.

## Key Classes

- `CompositeSelect<TRoot, TResult>`: The entry point for composite queries, initializing the query with a root table.
- `CompositeJoin<TRoot, TJoin, TResult>`: Represents a join between two tables.
- `CompositeJoin<TRoot, TPrevJoin, TJoin, TResult>`: Supports additional joins with type safety.
- `CompositeWhere<TRoot, TResult>`, `CompositeWhere<TRoot, TJoin, TResult>`, etc.: Where clauses for different join combinations.
- `PSqlCompositeExtensions`: Static methods to create composite queries for PostgreSQL.

## Usage Example

```csharp
// Create schemas for each model
var schemaA = DataSchema.Create<ModelA>("schema_a");
var schemaB = DataSchema.Create<ModelB>("schema_b");
var schemaC = DataSchema.Create<ModelC>("schema_c");

// Build a composite query with multiple joins
var query = PSqlCompositeExtensions.SelectComposite<ModelA, ModelA>(
    a => a, 
    schemaA)
    .Join<ModelB>(
        schemaB, 
        (a, b) => a.BId == b.Id)
    .Join<ModelC>(
        schemaC, 
        (b, c) => b.CId == c.Id)
    .Where((a, b, c) => !a.Deleted && !b.Deleted && !c.Deleted);

// Convert to SQL 
string sql = query.ToSql();
```

## Benefits

1. **Strong Typing**: All models and expressions are strongly typed
2. **Fluent API**: Chain methods to build complex queries
3. **Composite Models**: Handle relationships between multiple models
4. **Reuse Existing Code**: Built on top of the existing ExpressionToSql framework
5. **Arbitrary Join Depth**: Support for an arbitrary number of joins

## Implementation Notes

The implementation uses a recursive generic type structure to maintain type safety across multiple joins. Each join level adds its type to the generic signature, allowing the query builder to track all involved types throughout the query construction process.

The system uses the existing ExpressionToSql framework's underlying query building functionality while adding a layer to support strongly-typed composite models with multiple tables. 