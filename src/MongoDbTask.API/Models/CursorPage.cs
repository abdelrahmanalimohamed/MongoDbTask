namespace MongoDbTask.API.Models
{
    public sealed record CursorPage<T>(IReadOnlyList<T> Items, int? NextCursor);
}