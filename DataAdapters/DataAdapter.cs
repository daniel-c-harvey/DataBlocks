using Core;

namespace DataAccess
{
    public abstract class DataAdapter<TDatabase, TDataAccess, TQueryBuilder, TModel> : IDataAdapter<TModel>
        where TDatabase : class
        where TDataAccess : IDataAccess<TDatabase>
        where TQueryBuilder : IQueryBuilder<TDatabase>
        where TModel : IModel
    {
        protected TDataAccess DataAccess;
        protected TQueryBuilder QueryBuilder;
        protected DataSchema Schema;

        public DataAdapter(TDataAccess dataAccess, TQueryBuilder queryBuilder, DataSchema schema)
        {
            DataAccess = dataAccess;
            QueryBuilder = queryBuilder;
            Schema = schema;
        }

        ResultContainer<TModel> IDataAdapter<TModel>.GetByID(int id)
        {
            throw new NotImplementedException();
        }

        public ResultContainer<IEnumerable<TModel>> GetPage(int pageIndex, int pageSize)
        {
            var modelResults = new ResultContainer<IEnumerable<TModel>>();
            try
            {
                modelResults = DataAccess.ExecQuery(QueryBuilder.BuildRetrieve<TModel>(Schema.Collection, pageIndex, pageSize));
                return modelResults;
            }
            catch (Exception ex) 
            {
                return modelResults.Fail(ex.Message);
            }
        }

        public Result Delete(TModel model)
        {
            return DataAccess.ExecNonQuery(QueryBuilder.BuildDelete(Schema.Collection, model));
        }


        public Result Insert(TModel model)
        {
            try
            {
                DataAccess.ExecNonQuery(QueryBuilder.BuildInsert(Schema.Collection, model));
            }
            catch (Exception e) { return Result.CreateFailResult($"Database error: {e.Message}"); }
            return Result.CreatePassResult();
        }

        public Result Insert(IEnumerable<TModel> models)
        {
            throw new NotImplementedException();
        }

        
        public Result Update(TModel model)
        {
            try
            {
                DataAccess.ExecNonQuery(QueryBuilder.BuildReplace(Schema.Collection, model));
            }
            catch (Exception e) { return Result.CreateFailResult($"Database error: {e.Message}"); }
            return Result.CreatePassResult();
        }

        
    }
}
