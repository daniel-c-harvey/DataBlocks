namespace DataBlocks.DataAccess
{
    public interface IDataQuery<TDatabase, TReturn>
    {
        Func<TDatabase, Task<TReturn>> Query { get; }
    }
}
