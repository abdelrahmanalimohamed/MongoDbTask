using MongoDbTask.API.Models;

namespace MongoDbTask.API.Services
{
    public interface IUserImportService
    {
        Task<ImportResult> ImportAsync(
            CancellationToken cancellationToken);
    }
}
