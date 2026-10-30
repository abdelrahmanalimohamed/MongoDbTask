namespace MongoDbTask.API.Models
{
    public sealed record ImportResult(
      int Processed,
      int PagesFetched,
      int TotalAvailable);
}
