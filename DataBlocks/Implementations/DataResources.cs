namespace DataAccess
{
    public class DataResources<TDatabase, TDataAccess, TQueryBuilder> 
        : IDataResources<TDatabase, TDataAccess, TQueryBuilder> 
        where TDataAccess : IDataAccess<TDatabase>
        where TQueryBuilder : IQueryBuilder<TDatabase>
    {
        public TDataAccess DataAccess { get; set; }
        public TQueryBuilder QueryBuilder { get; set; }

        public DataResources(TDataAccess dataAccess, TQueryBuilder queryBuilder)
        {
            DataAccess = dataAccess;
            QueryBuilder = queryBuilder;
        }
    }
}
