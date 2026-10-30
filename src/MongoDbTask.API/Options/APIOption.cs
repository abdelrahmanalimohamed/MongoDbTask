namespace MongoDbTask.API.Options
{
    public sealed class APIOption
    {
        public const string SectionName = "APIOption";

        public string URL { get; init; } = string.Empty;
    }
}