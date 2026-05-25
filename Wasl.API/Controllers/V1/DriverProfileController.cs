using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wasl.Application.Common;
using Wasl.Application.Features.DriverProfiles.Commands.SubmitProfile;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/driver-profile")]
    [ApiController]
    [Tags("Driver")]
    public class DriverProfileController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DriverProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("submit")]
        [Tags("Driver Profile")]
        public async Task<ActionResult<ApiResponse<string>>> SubmitProfile([FromForm] SubmitDriverProfileCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}