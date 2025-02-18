
namespace DataAccess
{
    public class DataSchema
    {
        public string Collection { get; }

        public DataSchema(string collection)
        {
            Collection = collection;
        }
    }
}