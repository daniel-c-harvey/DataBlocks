using System.Linq.Expressions;
using DataBlocks.DataAccess;
using NetBlocks.Models;

namespace DataBlocks.DataAdapters
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

        public async Task<ResultContainer<TModel>> GetByID(long id)
        {
            var modelResults = new ResultContainer<TModel>();
            try
            {
                modelResults = await DataAccess.ExecQueryOne(QueryBuilder.BuildRetrieve<TModel>(Schema, id));
                return modelResults;
            }
            catch (Exception ex) 
            {
                return modelResults.Fail(ex.Message);
            }
        }

        public async Task<ResultContainer<IEnumerable<TModel>>> GetAll()
        {
            var modelResults = new ResultContainer<IEnumerable<TModel>>();
            try
            {
                modelResults = await DataAccess.ExecQuery(QueryBuilder.BuildRetrieve<TModel>(Schema));
                return modelResults;
            }
            catch (Exception ex) 
            {
                return modelResults.Fail(ex.Message);
            }
        }

        public async Task<ResultContainer<IEnumerable<TModel>>> GetWhereIn<TKey>(Expression<Func<TModel, TKey>> keySelector, IList<TKey> keys)
        {
            var modelResults = new ResultContainer<IEnumerable<TModel>>();
            try
            {
                modelResults = await DataAccess.ExecQuery(QueryBuilder.BuildRetrieve(Schema, keySelector, keys));
                return modelResults;
            }
            catch (Exception ex) 
            {
                return modelResults.Fail(ex.Message);
            }
        }
        
        public async Task<ResultContainer<IEnumerable<TModel>>> GetPage(int pageIndex, int pageSize)
        {
            var modelResults = new ResultContainer<IEnumerable<TModel>>();
            try
            {
                modelResults = await DataAccess.ExecQuery(QueryBuilder.BuildRetrieve<TModel>(Schema, pageIndex, pageSize));
                return modelResults;
            }
            catch (Exception ex) 
            {
                return modelResults.Fail(ex.Message);
            }
        }

        public async Task<ResultContainer<IEnumerable<TModel>>> GetByPredicate(Expression<Func<TModel, bool>> predicate)
        {
            var modelResults = new ResultContainer<IEnumerable<TModel>>();
            try
            {
                modelResults = await DataAccess.ExecQuery(QueryBuilder.BuildRetrieve(Schema, predicate));
                return modelResults;
            }
            catch (Exception ex) 
            {
                return modelResults.Fail(ex.Message);
            }
        }

        public async Task<Result> Delete(TModel model)
        {
            return await DataAccess.ExecNonQuery(QueryBuilder.BuildDelete(Schema, model));
        }

        public async Task<Result> Insert(TModel model)
        {
            try
            {
                Model.PrepareForInsert(model);
                await DataAccess.ExecNonQuery(QueryBuilder.BuildInsert(Schema, model));
            }
            catch (Exception e) { return Result.CreateFailResult($"Database error: {e.Message}"); }
            return Result.CreatePassResult();
        }

        public async Task<Result> Insert(IEnumerable<TModel> models)
        {
            throw new NotImplementedException();
        }
        
        public async Task<Result> Update(TModel model)
        {
            try
            {
                await DataAccess.ExecNonQuery(QueryBuilder.BuildReplace(Schema, model));
            }
            catch (Exception e) { return Result.CreateFailResult($"Database error: {e.Message}"); }
            return Result.CreatePassResult();
        }
    }
}
