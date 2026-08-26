using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Features.Tracking.Commands.UpdateDriverLocation;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TrackingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TrackingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Updates the driver's current GPS location while an active ride is in progress.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        ///
        /// The driver's ID is extracted from the JWT token. The endpoint verifies the driver
        /// has an active ride (Accepted/Arrived/InProgress), updates the Redis GEO index,
        /// records a server-side timestamp, and broadcasts the location to the rider via SignalR.
        /// </remarks>
        /// <param name="command">Contains the driver's current latitude and longitude.</param>
        /// <returns>A confirmation that the location was updated and broadcast.</returns>
        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("location")]
        [Tags("Tracking")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateDriverLocationCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
