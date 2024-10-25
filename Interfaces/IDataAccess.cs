using Core;

namespace DataAccess
{
    public interface IDataAccess<TDatabase>
    {
        ResultContainer<string> GetConnectionString();
        ResultContainer<string> GetDatabaseName();
        ResultContainer<IEnumerable<string>> GetDatabaseNames();
        Result ChangeConnection(Connection connection, string databaseName);
        ResultContainer<IEnumerable<TModel>> ExecQuery<TModel>(IDataQuery<TDatabase, ResultContainer<IEnumerable<TModel>>> query);
        ResultContainer<TModel> ExecQueryOne<TModel>(IDataQuery<TDatabase, ResultContainer<TModel>> query);
        Result ExecNonQuery(IDataQuery<TDatabase, Result> query);
    }
}
