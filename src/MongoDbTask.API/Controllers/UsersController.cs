using Microsoft.AspNetCore.Mvc;
using MongoDbTask.API.Models;
using MongoDbTask.API.Services;

namespace MongoDbTask.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserImportService _importService;
        private readonly UserSearchService _searchService;
        public UsersController(UserImportService importService, UserSearchService searchService)
        {
            _importService = importService;
            _searchService = searchService;
        }

        [HttpPost("import")]
        public async Task<ActionResult<ImportResult>> Import(
           CancellationToken cancellationToken)
        {
            var result = await _importService.ImportAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<CursorPage<UserResponse>>> Search(
            [FromQuery] UserSearchRequest request,
            CancellationToken cancellationToken)
        {
            var page = await _searchService.SearchAsync(request, cancellationToken);
            return Ok(page);
        }
    }
}
