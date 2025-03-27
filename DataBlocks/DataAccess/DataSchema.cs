
using ScheMigrator.Migrations;
using NetBlocks.Models;

namespace DataBlocks.DataAccess
{
    public class DataSchema
    {
        public string SchemaName { get; }
        public string CollectionName { get; }

        public DataSchema(string schemaName, string collectionName)
        {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
            CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        }

        /// <summary>
        /// Gets the fully qualified name for databases that support schemas (e.g., PostgreSQL)
        /// </summary>
        public string GetFullyQualifiedName()
        {
            return $"\"{SchemaName}\".\"{CollectionName}\"";
        }

        /// <summary>
        /// Gets just the collection name for databases that don't support schemas (e.g., MongoDB)
        /// </summary>
        public string GetCollectionName()
        {
            return CollectionName;
        }

        public static DataSchema Create<TModel>(string schemaName)
        {
            var collectionName = ScheModelUtil.GetTableName(typeof(TModel));
            return new DataSchema(schemaName, collectionName);
        }
    }
}