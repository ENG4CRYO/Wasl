using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Features.Rides.Commands;
using Wasl.Application.Features.Rides.Commands.AcceptRide;
using Wasl.Application.Features.Rides.Commands.CompleteRide;
using Wasl.Application.Features.Rides.Commands.DriverArrived;
using Wasl.Application.Features.Rides.Commands.RequestRide;
using Wasl.Application.Features.Rides.Commands.StartRide;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    /// <summary>
    /// Manages the core Ride Lifecycle, including requesting rides, dispatching, and accepting trips.
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class RidesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RidesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Initiates a new ride request by a Rider.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Rider
        /// 
        /// This endpoint takes the pickup and drop-off coordinates, creates a ride with 'Pending' status, 
        /// and triggers the dispatch system to notify nearby active drivers via SignalR.
        /// </remarks>
        /// <param name="command">Contains pickup/drop-off locations and optional ride details.</param>
        /// <returns>A confirmation message along with the newly created Ride ID.</returns>
        [Authorize(Roles = AspRoles.Rider)]
        [HttpPost("request")]
        [Tags("Rides")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RequestRide([FromBody] CreateRideRequestCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Allows a Driver to accept a pending ride request.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        /// 
        /// The system validates that the driver doesn't have any other active/pending rides. 
        /// If successful, the ride status changes to 'Accepted' and the rider is notified. 
        /// If another driver already accepted it (Race Condition), a 400 BadRequest is returned.
        /// </remarks>
        /// <param name="command">Contains the Ride ID that the driver wishes to accept.</param>
        /// <returns>A success message if the ride is assigned, or an error if already taken.</returns>
        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("accept")]
        [Tags("Rides")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AcceptRide([FromBody] AcceptRideCommand command)
        {

            var result = await _mediator.Send(command);

            if (result.Succeeded)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }


        /// <summary>
        /// Marks the ride as 'Arrived' when the driver reaches the pickup location.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        /// 
        /// Validates that the ride is currently in the 'Accepted' state and belongs to the requesting driver, 
        /// then transitions it to 'Arrived' to notify the rider.
        /// </remarks>
        /// <param name="id">The unique identifier of the Ride.</param>
        /// <returns>A success message indicating the status update.</returns>
        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("{id}/arrive")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DriverArrived(string id)
        {
            var command = new DriverArrivedCommand
            {
                RideId = id
            };

            var result = await _mediator.Send(command);

            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }


        /// <summary>
        /// Marks the ride as 'Completed' when the trip is successfully finished.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        /// 
        /// Validates that the ride is not already completed or cancelled, and belongs to the requesting driver, 
        /// then transitions it to 'Completed' to free up the driver for new requests.
        /// </remarks>
        /// <param name="id">The unique identifier of the Ride.</param>
        /// <returns>A success message indicating the ride has been completed.</returns>
        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("{id}/complete")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CompleteRide(string id)
        {

            var command = new CompleteRideCommand
            {
                RideId = id,
            };

            var result = await _mediator.Send(command);

            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Marks the ride as 'InProgress' when the trip actually starts.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        /// 
        /// Validates that the ride is currently in the 'Arrived' state and belongs to the requesting driver, 
        /// then transitions it to 'InProgress' and records the start time to track the actual trip duration.
        /// </remarks>
        /// <param name="id">The unique identifier of the Ride.</param>
        /// <returns>A success message indicating the ride has started.</returns>
        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("{id}/start")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> StartRide(string id)
        {
            var command = new StartRideCommand
            {
                RideId = id,
            };

            var result = await _mediator.Send(command);

            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}