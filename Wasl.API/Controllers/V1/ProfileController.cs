using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wasl.API.Extensions;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Profile;
using Wasl.Application.Features.Profile.Commands.UpdateRiderPhoto;
using Wasl.Application.Features.Profile.Queries.GetDriverProfile;
using Wasl.Application.Features.Profile.Queries.GetRiderProfile;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = AspRoles.Driver)]
        [Tags("Profile")]
        [HttpGet("driver")]
        public async Task<ActionResult<ApiResponse<DriverProfileDto>>> GetDriverProfile()
        {
            var result = await _mediator.Send(new GetDriverProfileQuery());
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = AspRoles.Rider)]
        [Tags("Profile")]
        [HttpGet("rider")]
        public async Task<ActionResult<ApiResponse<RiderProfileDto>>> GetRiderProfile()
        {
            var result = await _mediator.Send(new GetRiderProfileQuery());
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = AspRoles.Rider)]
        [Tags("Profile")]
        [HttpPut("rider/photo")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateRiderPhoto(IFormFile photo)
        {
            var command = new UpdateRiderPhotoCommand
            {
                Photo = photo.ToUploadedFile()!
            };

            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}
