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
        public UsersController(UserImportService importService)   
        {
            _importService = importService;
        }

        [HttpPost("import")]
        public async Task<ActionResult<ImportResult>> Import(
           CancellationToken cancellationToken)
        {
            var result = await _importService.ImportAsync(cancellationToken);
            return Ok(result);
        }
    }
}
