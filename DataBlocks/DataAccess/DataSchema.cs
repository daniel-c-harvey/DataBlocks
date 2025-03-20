
using DataBlocks.Migrations;
using NetBlocks.Models;

namespace DataBlocks.DataAccess
{
    public class DataSchema
    {
        public string SchemaName { get; }
        public string CollectionName { get; }

        public DataSchema(string schemaName, string collectionName)
        {
            SchemaName = schemaName;
            CollectionName = collectionName;
        }

        public static DataSchema Create<TModel>(string schemaName)
        {
            var collectionName = ScheModelUtil.GetTableName(typeof(TModel));
            return new DataSchema(schemaName, collectionName);
        }
    }
}