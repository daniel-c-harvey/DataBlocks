using DataBlocks.DataAccess.Mongo;
using DataBlocks.DataAccess.Postgres;
using MongoDB.Driver;
using System;

namespace DataBlocks.DataAccess;

public static class DataAccessFactory
{
    public static IDataAccess<TDatabase> Create<TClient, TDatabase>(string connectionString, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));
        
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentNullException(nameof(databaseName));

        // Check the type of TDatabase to determine which DataAccess to create
        if (typeof(IMongoDatabase).IsAssignableFrom(typeof(TDatabase)))
        {
            return (IDataAccess<TDatabase>)new MongoDataAccess(connectionString, databaseName);
        }
        else if (typeof(IPostgresDatabase).IsAssignableFrom(typeof(TDatabase)))
        {
            return (IDataAccess<TDatabase>)new PostgresDataAccess(connectionString, databaseName);
        }
        else
        {
            throw new ArgumentException($"Unsupported database type: {typeof(TDatabase).Name}");
        }
    }
} 