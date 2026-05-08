using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Threading.Tasks;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Features.Auth.Commands.InitiateRegistration;
using Wasl.Application.Features.Auth.Commands.VerifyRegistration;
using Wasl.Application.Features.Auth.Commands.Login;
using Wasl.Application.Features.Auth.Commands.RefreshToken;
using Wasl.Application.Features.Auth.Commands.RevokeToken;
using Wasl.Application.Features.Auth.Commands.ForgotPassword;
using Wasl.Application.Features.Auth.Commands.ResetPassword;

namespace Wasl.API.Controllers.V1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Registration Flow (OTP Based)
        [HttpPost("initiate-registration")]
        public async Task<IActionResult> InitiateRegistration([FromBody] InitiateRegistrationCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("verify-registration")]
        public async Task<IActionResult> VerifyRegistration([FromBody] VerifyRegistrationCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return BadRequest(result);

           
            return Ok(result);
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