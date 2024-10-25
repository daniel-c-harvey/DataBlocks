namespace DataAccess
{
    internal interface IDatabaseConnection<TDatabase>
    {
        string DatabaseName { get; }
        IDatabase<TDatabase> Database { get; }
    }
}