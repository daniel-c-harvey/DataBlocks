
namespace DataAccess
{
    public interface IDataQuery<TDatabase, TReturn>
    {
        Func<TDatabase, TReturn> Query { get; }
    }
}
