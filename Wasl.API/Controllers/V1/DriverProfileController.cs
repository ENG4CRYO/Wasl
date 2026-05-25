using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wasl.Application.Common;
using Wasl.Application.Features.DriverProfiles.Commands.SubmitProfile;
using Wasl.Application.Interfaces.Common;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/driver-profile")]
    [ApiController]
    [Tags("Driver")]
    public class DriverProfileController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public DriverProfileController(IMediator mediator, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("submit")]
        [Tags("Driver Profile")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<string>>> SubmitProfile([FromForm] SubmitDriverProfileCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        [Tags("Driver Profile")]
        [HttpGet("test-claims")]
        [Authorize] 
        public IActionResult TestClaims()
        {
           
            var claims = HttpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToList();

            return Ok(new
            {
                ControllerUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value,
                ServiceUserId = _currentUserService.UserId 
            });
        }
    }
}