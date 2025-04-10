using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Postgres;
using NetBlocks.Models;

namespace DataBlocks.DataAdapters
{
    public class PostgresCompositeDataAdapter<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel> 
        : CompositeDataAdapter<IPostgresDatabase, PostgresDataAccess, PostgresCompositeQueryBuilder, TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
    {
        public PostgresCompositeDataAdapter(PostgresDataAccess dataAccess, PostgresCompositeQueryBuilder queryBuilder)
            : base(dataAccess, queryBuilder)
        {
        }
    }
} 