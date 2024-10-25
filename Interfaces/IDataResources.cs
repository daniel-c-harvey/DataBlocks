using DataAccess;

namespace DataAccess
{
    public interface IDataResources<TDatabase, TDataAccess, TQueryBuilder>
        where TDataAccess : IDataAccess<TDatabase>
        where TQueryBuilder : IQueryBuilder<TDatabase>
    {
        TDataAccess DataAccess { get; set; }
        TQueryBuilder QueryBuilder { get; set; }
    }
}