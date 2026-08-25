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
using Wasl.Application.Features.Rides.Commands.ChangePaymentMethod;
using Wasl.Core.Enums;
using Wasl.Application.Features.Rides.Commands.CompleteRide;
using Wasl.Application.Features.Rides.Commands.DriverArrived;
using Wasl.Application.Features.Rides.Commands.RequestRide;
using Wasl.Application.Features.Rides.Commands.ReviewRide;
using Wasl.Application.Features.Rides.Commands.StartRide;
using Wasl.Application.Features.Rides.Queries.EstimateFare;
using Wasl.Application.Features.Rides.Queries.GetMyActiveRide;
using Wasl.Application.Features.Rides.Queries.GetRideById;
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
        /// 
        /// **PaymentMethod Enum Values:**
        /// 
        /// | Value | Description |
        /// |-------|-------------|
        /// | `Cash` (1) | Pay with cash upon completion. Company commission deducted from driver wallet. |
        /// | `Card` (2) | Invisible payments flow. Rider must call <c>POST /payments/tokenize</c> first to get a <c>paymentToken</c>, then include it here. The driver never sees card details. |
        /// | `Wallet` (3) | Pay using the rider's wallet balance. System checks balance before creating the ride; insufficient balance is rejected. |
        /// 
        /// **Invisible Payments Flow (Card):**
        /// 
        /// ```text
        /// 1. Rider calls POST /payments/tokenize → receives a GUID token
        /// 2. Rider calls POST /rides/request with paymentMethod=2 and paymentToken="guid"
        /// 3. Driver completes ride → system processes payment using the stored token
        /// ```
        /// 
        /// **Note:** The <c>paymentToken</c> is only persisted when <c>PaymentMethod</c> is <c>Card</c>.
        /// For <c>Cash</c> or <c>Wallet</c>, it is ignored even if provided.
        /// </remarks>
        /// <param name="command">Contains pickup/drop-off locations, payment method, and optional payment token for card payments.</param>
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
        /// 
        /// **No request body required.** The <c>PaymentToken</c> was already stored on the ride
        /// at request time (invisible payments flow), so the driver only sends the ride ID.
        /// 
        /// **Financial Settlement by PaymentMethod:**
        /// 
        /// | PaymentMethod | Rider | Driver |
        /// |---------------|-------|--------|
        /// | `Cash` | Pays cash to driver directly | Company commission deducted from wallet (may go negative) |
        /// | `Card` | Card processed via stored token (4242=success, 5555=insufficient funds, 1111=expired) | Net earnings (fare − commission) added to wallet |
        /// | `Wallet` | Fare deducted from wallet balance | Fare credited to wallet, then commission deducted (may go negative) |
        /// 
        /// A <c>WalletTransaction</c> ledger entry is created for every balance change.
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
            var command = new CompleteRideCommand { RideId = id };

            var result = await _mediator.Send(command);

            if (!result.Succeeded)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Changes the payment method of an in-progress ride (fallback when card is declined).
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        /// 
        /// **Fallback for failed card/wallet payments.** When the rider's card is declined or wallet balance 
        /// is insufficient at completion time, the driver can switch to Cash so the ride can still be completed.
        /// 
        /// **How it works:**
        /// 
        /// ```text
        /// 1. Driver attempts POST /rides/{id}/complete
        /// 2. Card/Wallet payment fails → error returned
        /// 3. Driver calls POST /rides/{id}/change-payment { "newPaymentMethod": 1 }
        /// 4. ride.PaymentMethod is set to Cash
        /// 5. Driver calls POST /rides/{id}/complete again
        /// 6. Cash branch runs: company commission deducted from driver wallet only
        /// ```
        /// 
        /// **Request body:** Only <c>newPaymentMethod</c> is required (e.g. <c>1</c> for Cash).
        /// The <c>RideId</c> is taken from the URL route, not the body.
        /// 
        /// **Note:** Switching from Card/Wallet to Cash only changes the settlement method.
        /// The stored <c>PaymentToken</c> (if Card) is not cleared but is safely ignored
        /// since the completion handler checks <c>ride.PaymentMethod</c> at runtime.
        /// </remarks>
        /// <param name="id">The unique identifier of the Ride.</param>
        /// <param name="request">Contains the new payment method (typically 1 = Cash).</param>
        /// <returns>A success message indicating the payment method was updated.</returns>
        [Authorize(Roles = AspRoles.Driver)]
        [HttpPost("{id}/change-payment")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangePaymentMethod(Guid id, [FromBody] ChangePaymentMethodRequest request)
        {
            var command = new ChangeRidePaymentMethodCommand
            {
                RideId = id,
                NewPaymentMethod = request.NewPaymentMethod
            };
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
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
        /// Gets the current user's active ride (Rider or Driver).
        /// </summary>
        /// <remarks>
        /// **Roles Required:** Rider, Driver
        /// 
        /// **State-recovery endpoint (source of truth).** Returns the caller's ride currently in
        /// `Pending`, `Accepted`, `Arrived`, or `InProgress` status with full details, or `null` data if no active ride exists.
        /// 
        /// Call this when the app starts, restarts, or after a long SignalR outage to restore
        /// the authoritative ride state. Do not rely on local storage or in-memory UI state.
        /// </remarks>
        /// <returns>The active ride snapshot, or null if the user has no active ride.</returns>
        [Authorize(Roles = "Rider,Driver")]
        [HttpGet("active")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<ActiveRideDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyActiveRide()
        {
            var result = await _mediator.Send(new GetMyActiveRideQuery());
            return Ok(result);
        }

        /// <summary>
        /// Gets full details of a specific ride (participant only).
        /// </summary>
        /// <remarks>
        /// **Roles Required:** Rider, Driver
        /// 
        /// **State-recovery endpoint.** Returns the authoritative ride snapshot including live driver
        /// location (from Redis). Only the Rider or assigned Driver of the ride may call it;
        /// non-participants receive a failure response.
        /// </remarks>
        /// <param name="id">The unique identifier of the Ride.</param>
        /// <returns>The ride snapshot.</returns>
        [Authorize(Roles = "Rider,Driver")]
        [HttpGet("{id:guid}")]
        [Tags("Rides")]
        [ProducesResponseType(typeof(ApiResponse<ActiveRideDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ActiveRideDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRideById(Guid id)
        {
            var result = await _mediator.Send(new GetRideByIdQuery { RideId = id });
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