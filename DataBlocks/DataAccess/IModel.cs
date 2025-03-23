using ScheMigrator.Migrations;

namespace DataBlocks.DataAccess
{
    public interface IModel : IModel<long> { }
    
    public interface IModel<TKey>
    {
        [ScheKey("id")]
        TKey ID { get; set; }
        [ScheData("deleted")]
        bool Deleted { get; set; }
        [ScheData("created")]
        DateTime Created { get; set; }
        [ScheData("modified")]
        DateTime Modified { get; set; }
    }
    
    public static class Model
    {
        public static void PrepareForInsert(IModel model)
        {
            model.Created = DateTime.Now;
        }
    
        public static void PrepareForUpdate(IModel model)
        {
            model.Modified = DateTime.Now;
        }
    }
}
