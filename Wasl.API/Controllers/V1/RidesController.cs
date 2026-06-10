using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Wasl.Application.Features.Rides.Commands;
using Wasl.Application.Features.Rides.Commands.RequestRide;
using Wasl.Core.Constants;

namespace Wasl.API.Controllers;

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


    [Authorize(Roles = AspRoles.Rider)]
    [HttpPost("request")]
    [Tags("Rides")]
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

    [Authorize(Roles = AspRoles.Driver)]
    [HttpPost("accept")]
    [Tags("Rides")]
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

        return BadRequest(new { Message = "عذراً، تم قبول الرحلة من قبل سائق آخر." });
    }
}