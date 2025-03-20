namespace DataBlocks.DataAccess
{
    public interface IDatabaseConnection<TDatabase>
    {
        string DatabaseName { get; }
        IDatabase<TDatabase> Database { get; }
    }
}