using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Features.Rides.Commands;
using Wasl.Application.Features.Rides.Commands.AcceptRide;
using Wasl.Application.Features.Rides.Commands.CancelRideByDriver;
using Wasl.Application.Features.Rides.Commands.CancelRideByRider;
using Wasl.Application.Features.Rides.Commands.CompleteRide;
using Wasl.Application.Features.Rides.Commands.DriverArrived;
using Wasl.Application.Features.Rides.Commands.RequestRide;
using Wasl.Application.Features.Rides.Commands.ReviewRide;
using Wasl.Application.Features.Rides.Commands.StartRide;
using Wasl.Application.Features.Rides.Queries.EstimateFare;
using Wasl.Application.Features.Rides.Queries.GetRideHistory;
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

            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

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
        /// <param name="id">Contains the Ride ID that the driver wishes to accept.</param>
        /// <returns>A success message if the ride is assigned, or an error if already taken.</returns>
        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("{id}/accept")]
        [Tags("Rides")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AcceptRide(string id)
        {
            var command = new AcceptRideCommand
            {
                RideId = id
            };

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


        /// <summary>
        /// Marks the ride as 'Cancelled' 
        /// </summary>
        /// <remarks>
        /// **Role Required:** Rider
        /// </remarks>
        [HttpPost("{id}/rider-cancel")]
        [Tags("Rides")]
        [Authorize(Roles = AspRoles.Rider)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CancelByRider(Guid id)
        {
            var result = await _mediator.Send(new CancelRideByRiderCommand { RideId = id });
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cancel a ride by the assigned Driver.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        /// </remarks>
        [HttpPost("{id}/driver-cancel")]
        [Authorize(Roles = AspRoles.Driver)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Tags("Rides")]
        public async Task<ActionResult<ApiResponse<bool>>> CancelByDriver(Guid id)
        {
            var result = await _mediator.Send(new CancelRideByDriverCommand { RideId = id });
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Estimates the fare and distance for a ride before requesting it.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Rider
        /// 
        /// This endpoint calculates the straight-line distance between the pickup and drop-off 
        /// coordinates, and returns the estimated price and distance based on the system's pricing rules.
        /// It is typically called when the rider selects their destination, before confirming the ride.
        /// </remarks>
        /// <param name="pickupLat">The latitude of the pickup location.</param>
        /// <param name="pickupLng">The longitude of the pickup location.</param>
        /// <param name="dropoffLat">The latitude of the drop-off location.</param>
        /// <param name="dropoffLng">The longitude of the drop-off location.</param>
        /// <returns>An estimated price, distance, and currency for the ride.</returns>
        [Authorize(Roles = AspRoles.Rider)]
        [HttpGet("estimate")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<RideEstimateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<RideEstimateDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<RideEstimateDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<RideEstimateDto>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> EstimateFare(
            [FromQuery] double pickupLat,
            [FromQuery] double pickupLng,
            [FromQuery] double dropoffLat,
            [FromQuery] double dropoffLng)
        {
            var query = new EstimateRideFareQuery
            {
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DropoffLat = dropoffLat,
                DropoffLng = dropoffLng
            };

            var result = await _mediator.Send(query);

            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }


        /// <summary>
        /// Gets the ride history for the current user (Rider or Driver).
        /// </summary>
        /// <remarks>
        /// **Roles Required:** Rider, Driver
        /// 
        /// Returns a paginated list of past rides (Completed / Cancelled only).
        /// Riders see rides where they are the rider; Drivers see rides where they are the driver.
        /// </remarks>
        /// <param name="query">Pagination parameters (PageNumber, PageSize).</param>
        /// <returns>A paginated list of ride history entries.</returns>
        [Authorize(Roles = "Rider,Driver")]
        [HttpGet("history")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<PagedList<RideHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRideHistory([FromQuery] GetRideHistoryQuery query)
        {
            var result = await _mediator.Send(query);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Submits a review and rating for a completed ride.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Rider
        /// 
        /// Validates that the ride belongs to the caller, is completed, and has not been reviewed yet.
        /// The driver ID is securely inferred from the ride details on the server side.
        /// </remarks>
        /// <param name="id">The unique identifier of the Ride to review.</param>
        /// <param name="dto">Contains the rating (1-5) and an optional comment.</param>
        /// <returns>A success message indicating the review was saved.</returns>
        [Authorize(Roles = AspRoles.Rider)]
        [HttpPost("{id}/review")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ReviewRide(Guid id, [FromBody] ReviewRideRequestDto dto)
        {
            var command = new ReviewRideCommand
            {
                RideId = id,
                Rating = dto.Rating,
                Comment = dto.Comment
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