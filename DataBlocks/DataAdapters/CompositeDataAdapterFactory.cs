using System;
using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Postgres;
using NetBlocks.Models;

namespace DataBlocks.DataAdapters
{
    public static class CompositeDataAdapterFactory
    {
        public static ICompositeDataAdapter<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel> Create<TDatabase, TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(
            IDataAccess<TDatabase> dataAccess, 
            ICompositeQueryBuilder<TDatabase> queryBuilder)
            where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
            where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
            where TTargetModel : IConstituentModel<TTargetDataModel>
            where TDataModel : IModel
            where TLinkDataModel : IModel
            where TTargetDataModel : IModel
            where TDatabase : class
        {
            if (dataAccess.GetType().IsAssignableFrom(typeof(PostgresDataAccess)) && 
                queryBuilder.GetType().IsAssignableFrom(typeof(PostgresCompositeQueryBuilder)) &&
                typeof(TDatabase).IsAssignableFrom(typeof(IPostgresDatabase)))
            {
                return new PostgresCompositeDataAdapter<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(
                    (PostgresDataAccess)(object)dataAccess, 
                    (PostgresCompositeQueryBuilder)(object)queryBuilder);
            }
            
            // Add support for other database types as needed
            
            throw new ArgumentException($"No composite adapter available for the combination of {typeof(TDatabase).Name}");
        }
    }
} 