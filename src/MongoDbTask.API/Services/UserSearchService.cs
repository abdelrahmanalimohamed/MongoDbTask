using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbTask.API.DBContext;
using MongoDbTask.API.Models;
using System.Text.RegularExpressions;

namespace MongoDbTask.API.Services
{
    public class UserSearchService
    {
        private static readonly BsonDocument Projection = new()
        {
            { "_id", 0 },
            { "externalId", 1 },
            { "firstName", 1 },
            { "lastName", 1 },
            { "username", 1 },
            { "email", 1 },
            { "phone", 1 },
            { "age", 1 },
            { "gender", 1 },
            { "role", 1 },
            { "city", "$address.city" },
            { "country", "$address.country" }
        };
        private static readonly BsonDocument SortByExternalId = new("externalId", 1);

        private readonly MongoDbContext _database;

        public UserSearchService(MongoDbContext database)
        {
            _database = database;
        }

        public async Task<CursorPage<UserResponse>> SearchAsync(
            UserSearchRequest request,
            CancellationToken cancellationToken)
        {
            var filter = BuildFilter(request);

            var documents = await _database.Users
                .Find(filter)
                .Sort(SortByExternalId)
                .Project<BsonDocument>(Projection)
                .Limit(request.Limit + 1)
                .ToListAsync(cancellationToken);

            var hasMore = documents.Count > request.Limit;
            if (hasMore)
                documents.RemoveAt(documents.Count - 1);

            var users = new List<UserResponse>(documents.Count);
            foreach (var document in documents)
                users.Add(ToUserResponse(document));

            int? nextCursor = hasMore ? users[^1].Id : null;

            return new CursorPage<UserResponse>(users, nextCursor);
        }

        private static BsonDocument BuildFilter(UserSearchRequest request)
        {
            var filter = new BsonDocument();

            if (request.Cursor is { } cursor)
                filter["externalId"] = new BsonDocument("$gt", cursor);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var contains = ContainsIgnoreCase(request.Search);

                filter["$or"] = new BsonArray
                {
                    new BsonDocument("firstName", contains),
                    new BsonDocument("lastName", contains),
                    new BsonDocument("username", contains),
                    new BsonDocument("email", contains)
                };
            }

            AddEquals(filter, "gender", request.Gender);
            AddEquals(filter, "role", request.Role);
            AddEquals(filter, "address.city", request.City);
            AddEquals(filter, "address.country", request.Country);

            var ageRange = new BsonDocument();
            if (request.MinAge is { } minAge) ageRange["$gte"] = minAge;
            if (request.MaxAge is { } maxAge) ageRange["$lte"] = maxAge;
            if (ageRange.ElementCount > 0) filter["age"] = ageRange;

            return filter;
        }

        private static void AddEquals(BsonDocument filter, string field, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                filter[field] = new BsonRegularExpression($"^{Regex.Escape(value.Trim())}$", "i");
        }

        private static BsonRegularExpression ContainsIgnoreCase(string text) =>
            new(Regex.Escape(text.Trim()), "i");

        private static UserResponse ToUserResponse(BsonDocument document) => new(
            document["externalId"].AsInt32,
            document.GetValue("firstName", "").AsString,
            document.GetValue("lastName", "").AsString,
            document.GetValue("username", "").AsString,
            document.GetValue("email", "").AsString,
            document.GetValue("phone", "").AsString,
            document.GetValue("age", 0).ToInt32(),
            document.GetValue("gender", "").AsString,
            document.GetValue("role", "").AsString,
            document.GetValue("city", "").AsString,
            document.GetValue("country", "").AsString);
    }
}
