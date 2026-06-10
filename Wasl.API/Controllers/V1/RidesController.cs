using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Wasl.Application.Features.Rides.Commands;
using Wasl.Application.Features.Rides.Commands.RequestRide;
using Wasl.Application.Features.Rides.Commands.AcceptRide; // أضفنا هذا المسار
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
            var riderId = User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(riderId))
            {
                return Unauthorized(new { Message = "يجب تسجيل الدخول أولاً" });
            }

            command.RiderId = riderId;

            var rideId = await _mediator.Send(command);

            return Ok(new
            {
                Message = "تم استلام الطلب، جاري البحث عن أقرب السائقين...",
                RideId = rideId
            });
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
            var driverId = User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(driverId))
            {
                return Unauthorized(new { Message = "التوكن غير صالح أو لا يحتوي على معرف السائق" });
            }

            command.DriverId = driverId;

            var isSuccess = await _mediator.Send(command);

            if (isSuccess)
            {
                return Ok(new { Message = "مبروك، الرحلة من نصيبك!", RideId = command.RideId });
            }

            return BadRequest(new { Message = "عذراً، تم قبول الرحلة من قبل سائق آخر أو أنك مشغول برحلة حالية." });
        }
    }
}