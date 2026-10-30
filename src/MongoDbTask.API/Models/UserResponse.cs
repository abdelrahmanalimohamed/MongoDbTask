namespace MongoDbTask.API.Models
{
    public sealed record UserResponse(
        int Id,
        string FirstName,
        string LastName,
        string Username,
        string Email,
        string Phone,
        int Age,
        string Gender,
        string Role,
        string City,
        string Country);
}