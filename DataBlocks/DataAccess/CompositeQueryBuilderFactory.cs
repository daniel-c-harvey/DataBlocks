using System;
using DataBlocks.DataAccess.Postgres;
using MongoDB.Driver;

namespace DataBlocks.DataAccess;

public static class CompositeQueryBuilderFactory
{
        /// <summary>
        /// Creates a query builder instance appropriate for the specified database type.
        /// </summary>
        /// <typeparam name="TDatabase">The type of database to create a query builder for.</typeparam>
        /// <returns>An instance of IQueryBuilder compatible with the specified database type.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported database type is provided.</exception>
        public static ICompositeQueryBuilder<TDatabase> Create<TDatabase>()
        {
            // if (typeof(TDatabase) == typeof(IMongoDatabase))
            // {
            //     return (ICompositeQueryBuilder<TDatabase>)new MongoCompositeQueryBuilder();
            // }
            // else
            if (typeof(TDatabase) == typeof(IPostgresDatabase))
            {
                return (ICompositeQueryBuilder<TDatabase>)new PostgresCompositeQueryBuilder();
            }
            
            throw new ArgumentException($"Unsupported database type: {typeof(TDatabase).Name}");
        }
}
