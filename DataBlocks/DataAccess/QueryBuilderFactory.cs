using System;
using DataBlocks.DataAccess.Mongo;
using DataBlocks.DataAccess.Postgres;
using MongoDB.Driver;

namespace DataBlocks.DataAccess
{
    /// <summary>
    /// Factory for creating appropriate query builder instances based on the database type.
    /// </summary>
    public static class QueryBuilderFactory
    {
        /// <summary>
        /// Creates a query builder instance appropriate for the specified database type.
        /// </summary>
        /// <typeparam name="TDatabase">The type of database to create a query builder for.</typeparam>
        /// <returns>An instance of IQueryBuilder compatible with the specified database type.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported database type is provided.</exception>
        public static IQueryBuilder<TDatabase> Create<TDatabase>()
        {
            if (typeof(TDatabase) == typeof(IMongoDatabase))
            {
                return (IQueryBuilder<TDatabase>)new MongoQueryBuilder();
            }
            else if (typeof(TDatabase) == typeof(IPostgresDatabase))
            {
                return (IQueryBuilder<TDatabase>)new PostgresQueryBuilder();
            }
            
            throw new ArgumentException($"Unsupported database type: {typeof(TDatabase).Name}");
        }
    }
} 