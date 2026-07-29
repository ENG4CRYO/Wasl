using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Features.DriverEarnings.Queries.GetOverview;
using Wasl.Application.Interfaces.Common;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DriverEarningsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public DriverEarningsController(IMediator mediator, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Gets the driver's earnings overview for a given date range.
        /// </summary>
        /// <remarks>
        /// **Role Required:** Driver
        /// 
        /// Aggregates completed rides count, total net earnings, online minutes, and cash-out eligibility.
        /// 
        /// **Sample Request:**
        /// 
        ///     GET /api/v1/DriverEarnings/overview?startDate=2026-07-01&amp;endDate=2026-07-29
        /// </remarks>
        [Authorize(Roles = AspRoles.Driver)]
        [Tags("Driver Earnings")]
        [HttpGet("overview")]
        [ProducesResponseType(typeof(ApiResponse<DriverEarningsOverviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOverview([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var driverId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(driverId))
                return Unauthorized(ApiResponse<object>.Failure("User not found"));

            var query = new GetDriverEarningsOverviewQuery
            {
                DriverId = driverId,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await _mediator.Send(query);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}
