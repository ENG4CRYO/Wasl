using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Features.Payments.Commands.TokenizeCard;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    /// <summary>
    /// Handles payment tokenization for card payments (invisible payments flow).
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tokenizes a card for later use in ride payments (invisible payments flow).
        /// </summary>
        /// <remarks>
        /// **Role Required:** Rider
        /// 
        /// Called **before** requesting a ride. The rider submits their card details and receives a
        /// one-time-use payment token (GUID). This token is then included in the ride request
        /// (<c>POST /rides/request</c>) so the driver never sees the raw card information.
        /// 
        /// **Supported Test Cards (MockGateway):**
        /// 
        /// | Card Prefix | Tokenization | Payment Behavior |
        /// |-------------|-------------|------------------|
        /// | `4242` | ✅ Accepted | ✅ Payment succeeds |
        /// | `5555` | ✅ Accepted | ❌ Declined — Insufficient funds |
        /// | `1111` | ✅ Accepted | ❌ Declined — Expired card |
        /// | Any other | ❌ Rejected | N/A — Error returned |
        /// 
        /// **Tokens are single-use.** Once processed during ride completion, the token is consumed.
        /// A second attempt with the same token returns "Invalid or expired payment token."
        /// </remarks>
        /// <param name="command">Card details (number, expiry month/year, CVV).</param>
        /// <returns>An opaque payment token (GUID) to be sent with the ride request.</returns>
        [Authorize(Roles = AspRoles.Rider)]
        [HttpPost("tokenize")]
        [Tags("Payments")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> TokenizeCard([FromBody] TokenizeCardCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}
