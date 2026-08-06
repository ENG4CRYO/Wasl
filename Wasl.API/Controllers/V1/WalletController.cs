using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Wallet;
using Wasl.Application.Features.Wallet.Queries.GetDriverWalletBalance;
using Wasl.Application.Features.Wallet.Queries.GetRiderWalletBalance;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WalletController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gets the driver's current wallet balance.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        /// 
        /// Returns the current wallet balance of the authenticated driver.
        /// 
        /// **Sample Request:**
        /// 
        ///     GET /api/v1/wallet/driver/balance
        /// </remarks>
        [Authorize(Roles = AspRoles.Driver)]
        [Tags("Wallet")]
        [HttpGet("driver/balance")]
        [ProducesResponseType(typeof(ApiResponse<WalletBalanceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDriverBalance()
        {
            var result = await _mediator.Send(new GetDriverWalletBalanceQuery());
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Gets the rider's current wallet balance.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Rider
        /// 
        /// Returns the current wallet balance of the authenticated rider.
        /// 
        /// **Sample Request:**
        /// 
        ///     GET /api/v1/wallet/rider/balance
        /// </remarks>
        [Authorize(Roles = AspRoles.Rider)]
        [Tags("Wallet")]
        [HttpGet("rider/balance")]
        [ProducesResponseType(typeof(ApiResponse<WalletBalanceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRiderBalance()
        {
            var result = await _mediator.Send(new GetRiderWalletBalanceQuery());
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}
