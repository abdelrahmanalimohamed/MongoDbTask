using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDbTask.API.Options;
using System.Text.Json;

namespace MongoDbTask.API.Services
{
    public sealed class DummyJsonClient
    {
        private readonly HttpClient _httpClient;
        public DummyJsonClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<(IReadOnlyList<BsonDocument> Users, int Total)> GetUsersPageAsync(
               int limit,
               int skip,
               CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(
                $"users?limit={limit}&skip={skip}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            var root = json.RootElement;
            var total = root.GetProperty("total").GetInt32();
            var usersElement = root.GetProperty("users");

            var users = new List<BsonDocument>();

            foreach (var userElement in usersElement.EnumerateArray())
            {
                var document = BsonDocument.Parse(userElement.GetRawText());

                // DummyJSON uses "id"; MongoDB will generate its own "_id".
                var externalId = document["id"].ToInt32();
                document.Remove("id");
                document["externalId"] = externalId;

                users.Add(document);
            }

            return (users, total);
        }
    }
}
