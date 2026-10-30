using System.ComponentModel.DataAnnotations;

namespace MongoDbTask.API.Models
{
    public sealed class UserSearchRequest
    {
        public string? Search { get; init; }
        public string? Gender { get; init; }
        public string? Role { get; init; }
        public string? City { get; init; }
        public string? Country { get; init; }

        [Range(0, 150)]
        public int? MinAge { get; init; }

        [Range(0, 150)]
        public int? MaxAge { get; init; }

        [Range(1, 100)]
        public int Limit { get; init; } = 20;

        /// <summary>The NextCursor returned by the previous page (omit for the first page).</summary>
        public int? Cursor { get; init; }
    }
}
