using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Features.Auth.Commands.DriverRegistration;
using Wasl.Application.Features.Auth.Commands.DriverRegistration.InitiateDriverRegistration;
using Wasl.Application.Features.Auth.Commands.ForgotPassword;
using Wasl.Application.Features.Auth.Commands.Login;
using Wasl.Application.Features.Auth.Commands.RefreshToken;
using Wasl.Application.Features.Auth.Commands.ResetPassword;
using Wasl.Application.Features.Auth.Commands.RevokeToken;
using Wasl.Application.Features.Auth.Commands.RiderRegistration.InitiateRiderRegistration;
using Wasl.Application.Features.Auth.Commands.RiderRegistration.VerifyRiderRegistration;

namespace Wasl.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Rider Registration

        [HttpPost("rider/initiate-registration")]
        [Tags("Rider Authentication")]
        public async Task<ActionResult<ApiResponse<string>>> InitiateRiderRegistration([FromBody] InitiateRiderRegistrationCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPost("rider/verify-registration")]
        [Tags("Rider Authentication")]
        public async Task<ActionResult<ApiResponse<AuthModel>>> VerifyRiderRegistration([FromBody] VerifyRiderRegistrationCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        #endregion

        #region Driver Registration

        [HttpPost("driver/initiate-registration")]
        [Tags("Driver Authentication")]
        public async Task<ActionResult<ApiResponse<string>>> InitiateDriverRegistration([FromBody] InitiateDriverRegistrationCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPost("driver/verify-registration")]
        [Tags("Driver Authentication")]
        public async Task<ActionResult<ApiResponse<AuthModel>>> VerifyDriverRegistration([FromBody] VerifyDriverRegistrationCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        #endregion

        #region Login & Token Management

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Password Management Flow

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand request)
        {
            var result = await _mediator.Send(request);

            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion
    }
}