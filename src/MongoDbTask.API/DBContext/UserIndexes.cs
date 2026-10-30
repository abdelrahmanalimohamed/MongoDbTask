using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDbTask.API.DBContext
{
    public static class UserIndexes
    {
        public static async Task CreateAsync(
        IMongoCollection<BsonDocument> users,
        CancellationToken cancellationToken)
        {
            var indexes = new[]
            {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("externalId"),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_users_externalId"
                }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending("age")
                    .Ascending("company.department"),
                new CreateIndexOptions { Name = "ix_users_age_department" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("company.department"),
                new CreateIndexOptions { Name = "ix_users_department" })
            };

            await users.Indexes.CreateManyAsync(
                indexes,
                cancellationToken);
        }
    }
}
