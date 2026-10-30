using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbTask.API.Options;

namespace MongoDbTask.API.DBContext
{
    public class MongoDbContext
    {
        public IMongoCollection<BsonDocument> Users { get; }
        public MongoDbContext(IOptions<MongoDbOptions> options)
        {
            var settings = options.Value;

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException("MongoDb:ConnectionString is required.");

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                throw new InvalidOperationException("MongoDb:DatabaseName is required.");

            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            Users = database.GetCollection<BsonDocument>(
                string.IsNullOrWhiteSpace(settings.UsersCollectionName)
                    ? "users"
                    : settings.UsersCollectionName);
        }
    }
}
