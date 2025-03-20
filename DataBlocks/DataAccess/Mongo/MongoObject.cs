using DataAccess;
using MongoDB.Bson;

namespace DataBlocks.DataAccess.Mongo
{
    public class MongoObject<T> where T : IModel
    {
        public ObjectId _id { get; set; } = default!;

        private T _document = default!;
        public T Document { 
            get { return _document; }
            set 
            {
                _document = value;
                _id = new ObjectId(ObjectIDFromID(Document.ID));
            } 
        }

        private static byte[] ObjectIDFromID(long id) 
        {
            byte[] hash = new byte[12];
            foreach (int stage in new NumRange<int>(0, 11))
            {
                int shift = stage * 8;
                hash[stage] = (byte)((id & (0xFFL << shift)) >>> shift);
            }
            return hash;
        }

    }
}