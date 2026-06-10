using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wasl.Application.Common;
using Wasl.Application.Features.DriverProfiles.Commands.SubmitProfile;
using Wasl.Application.Interfaces.Common;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    /// <summary>
    /// Manages Driver Profile operations, including document uploads and profile status tracking.
    /// </summary>
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

        /// <summary>
        /// Submits the Driver's profile details and documents for review.
        /// </summary>
        /// <remarks>
        /// This endpoint requires `multipart/form-data` as it handles file uploads.
        /// Drivers must upload their required documents (e.g., Driver License, Vehicle Registration, Personal Photo).
        /// Upon successful submission, the driver's approval status will typically be set to 'Pending' awaiting Admin verification.
        /// </remarks>
        /// <param name="command">The form data containing driver details and attached files.</param>
        /// <returns>A success message indicating the profile has been submitted for review.</returns>
        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("submit")]
        [Tags("Driver Profile")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<string>>> SubmitProfile([FromForm] SubmitDriverProfileCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

    }
}