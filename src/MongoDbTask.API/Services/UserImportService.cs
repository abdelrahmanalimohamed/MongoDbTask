using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbTask.API.DBContext;
using MongoDbTask.API.Models;

namespace MongoDbTask.API.Services
{
    public sealed class UserImportService
    {
        private const int PageSize = 30;
        private readonly MongoDbContext _database;
        private readonly DummyJsonClient _dummyJsonClient;
        public UserImportService(
            MongoDbContext database,
            DummyJsonClient dummyJsonClient)
        {
            _database = database;
            _dummyJsonClient = dummyJsonClient;
        }

        public async Task<ImportResult> ImportAsync(
            CancellationToken cancellationToken)
        {
            await UserIndexes.CreateAsync(_database.Users, cancellationToken);

            var skip = 0;
            var total = int.MaxValue;
            var processed = 0;
            var pagesFetched = 0;

            while (skip < total)
            {
                var page = await _dummyJsonClient.GetUsersPageAsync(
                    PageSize, skip, cancellationToken);

                total = page.Total;
                pagesFetched++;

                if (page.Users.Count == 0)
                    break;

                var writes = new List<WriteModel<BsonDocument>>(page.Users.Count);

                foreach (var user in page.Users)
                {
                    var filter = new BsonDocument("externalId", user["externalId"]);
                    writes.Add(new ReplaceOneModel<BsonDocument>(filter, user)
                    {
                        IsUpsert = true
                    });
                }

                await _database.Users.BulkWriteAsync(
                    writes,
                    new BulkWriteOptions { IsOrdered = false },
                    cancellationToken);

                processed += page.Users.Count;
                skip += page.Users.Count;
            }

            return new ImportResult(processed, pagesFetched, total);
        }
    }
}
