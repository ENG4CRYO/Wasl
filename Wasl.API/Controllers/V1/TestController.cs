using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Wasl.api.Controllers.V1
{
    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[Controller]")]
    [ApiVersion("1.0")]
    public class TestController : ControllerBase
    {
        [HttpGet("protected")]
        public IActionResult GetProtectedData()
        {
            return Ok("This is protected data accessible only to authenticated users.");
        }

        [HttpGet("error")]
        public IActionResult GetError()
        {
            throw new Exception("This is a test exception for global error handling.");
        }
        public IActionResult TestCompression()
        {
            var hugeData = Enumerable.Range(1, 5000)
                .Select(index => new { Id = index, Summary = "Test data for size compersion" + Guid.NewGuid() })
                .ToList();

            return Ok(hugeData);
        }
    }
}
