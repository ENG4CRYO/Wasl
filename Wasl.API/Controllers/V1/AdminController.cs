using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;
using Wasl.Application.Features.Admin.Commands.ChangeDriverStatus;
using Wasl.Application.Features.Admin.Commands.ReviewDriverProfile;
using Wasl.Application.Features.Admin.Queries.GetAllDrivers;
using Wasl.Application.Features.Admin.Queries.GetPendingDriverDetails;
using Wasl.Application.Features.Admin.Queries.GetPendingDrivers;
using Wasl.Core.Constants;
using Wasl.Core.Enums;

namespace Wasl.API.Controllers.V1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Roles = AspRoles.Admin)]
    [Tags("Admin Operations")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get a paginated list of drivers pending approval (Lightweight).
        /// </summary>
        [HttpGet("pending-drivers")]
        public async Task<ActionResult<ApiResponse<PagedList<PendingDriverListDto>>>> GetPendingDrivers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetPendingDriversQuery { PageNumber = pageNumber, PageSize = pageSize };
            var result = await _mediator.Send(query);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get full details of a specific pending driver.
        /// </summary>
        [HttpGet("pending-drivers/{driverId}")]
        public async Task<ActionResult<ApiResponse<PendingDriverDetailsDto>>> GetPendingDriverDetails(string driverId)
        {
            var query = new GetPendingDriverDetailsQuery { DriverId = driverId };
            var result = await _mediator.Send(query);
            return result.Succeeded ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Approve or Reject a pending driver profile.
        /// </summary>
        [HttpPost("review-driver")]
        public async Task<ActionResult<ApiResponse<bool>>> ReviewDriverProfile([FromBody] ReviewDriverProfileCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get list of all drivers.
        /// </summary>
        [HttpGet("all-drivers")]
        public async Task<IActionResult> GetAllDrivers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DriverApprovalStatus? statusFilter = null)
        {
            var query = new GetAllDriversQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter
            };
            var response = await _mediator.Send(query);
            return response.Succeeded ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Change status of a driver .
        /// </summary>
        [HttpPut("change-driver-status")]
        public async Task<IActionResult> ChangeDriverStatus([FromBody] ChangeDriverStatusCommand command)
        {
            var response = await _mediator.Send(command);
            return response.Succeeded ? Ok(response) : BadRequest(response);
        }
    }
}