using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
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
    /// <summary>
    /// Manages Authentication, Registration, and Security Tokens for both Riders and Drivers.
    /// </summary>
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

        /// <summary>
        /// Step 1: Initiate Rider Registration (OTP Flow).
        /// </summary>
        /// <remarks>
        /// Receives basic rider info (Email/Phone) and sends a 6-digit OTP asynchronously. 
        /// Returns a 'RegisterToken' (GUID) which must be kept by the client for Step 2.
        /// </remarks>
        /// <param name="command">Rider's basic registration details.</param>
        /// <returns>A unique RegisterToken to proceed with verification.</returns>
        [HttpPost("rider/initiate-registration")]
        [Tags("Rider Auth")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<string>>> InitiateRiderRegistration([FromBody] InitiateRiderRegistrationCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Step 2: Verify OTP and Complete Rider Registration.
        /// </summary>
        /// <remarks>
        /// Takes the 'RegisterToken' received from Step 1 along with the 6-digit OTP sent to the user.
        /// If valid, creates the database record and issues access/refresh tokens.
        /// </remarks>
        /// <param name="command">Contains RegisterToken, OTP, and final account details.</param>
        /// <returns>Authentication model containing JWT and Refresh Token.</returns>
        [HttpPost("rider/verify-registration")]
        [Tags("Rider Auth")]
        [ProducesResponseType(typeof(ApiResponse<AuthModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthModel>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<AuthModel>>> VerifyRiderRegistration([FromBody] VerifyRiderRegistrationCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        #endregion

        #region Driver Registration

        /// <summary>
        /// Step 1: Initiate Driver Registration (OTP Flow).
        /// </summary>
        /// <remarks>
        /// Initiates the onboarding process for a new Driver. Sends a 6-digit OTP and returns a RegisterToken.
        /// </remarks>
        /// <param name="command">Driver's contact details to receive the OTP.</param>
        /// <returns>A unique RegisterToken to proceed with verification.</returns>
        [HttpPost("driver/initiate-registration")]
        [Tags("Driver Auth")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<string>>> InitiateDriverRegistration([FromBody] InitiateDriverRegistrationCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Step 2: Verify OTP and Complete Driver Registration.
        /// </summary>
        /// <remarks>
        /// Validates the OTP and creates the Driver profile. Note: Drivers might require admin approval before becoming active.
        /// </remarks>
        /// <param name="command">Contains RegisterToken, OTP, and driver-specific details (e.g., license).</param>
        /// <returns>Authentication model containing JWT and Refresh Token.</returns>
        [HttpPost("driver/verify-registration")]
        [Tags("Driver Auth")]
        [ProducesResponseType(typeof(ApiResponse<AuthModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthModel>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<AuthModel>>> VerifyDriverRegistration([FromBody] VerifyDriverRegistrationCommand command)
        {
            var result = await _mediator.Send(command);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        #endregion

        #region Login & Token Management

        /// <summary>
        /// Authenticates a user and issues security tokens.
        /// </summary>
        /// <remarks>
        /// Validates credentials (Email/Phone and Password). Returns a short-lived JWT Access Token and a long-lived Refresh Token.
        /// </remarks>
        /// <param name="request">User credentials.</param>
        /// <returns>Authentication model containing JWT and roles.</returns>
        [HttpPost("login")]
        [Tags("Common Authentication")]
        [ProducesResponseType(typeof(ApiResponse<AuthModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthModel>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return Unauthorized(result);

            return Ok(result);
        }

        /// <summary>
        /// Generates a new JWT using a valid Refresh Token.
        /// </summary>
        /// <remarks>
        /// Use this endpoint when the JWT Access Token expires to maintain the user session without forcing a re-login.
        /// </remarks>
        /// <param name="request">Contains the expired Access Token and the active Refresh Token.</param>
        /// <returns>A fresh Authentication model.</returns>
        [HttpPost("refresh-token")]
        [Tags("Common Authentication")]
        [ProducesResponseType(typeof(ApiResponse<AuthModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthModel>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Revokes an active Refresh Token.
        /// </summary>
        /// <remarks>
        /// Invalidates the given refresh token, effectively logging the user out from that specific device session.
        /// </remarks>
        /// <param name="request">The refresh token to be revoked.</param>
        /// <returns>Success status.</returns>
        [HttpPost("revoke-token")]
        [Tags("Common Authentication")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Password Management Flow

        /// <summary>
        /// Step 1: Request Password Reset.
        /// </summary>
        /// <remarks>
        /// Generates an OTP and sends it to the user's registered email/phone. Returns a 'ResetToken' for the next step.
        /// </remarks>
        /// <param name="request">The registered Email or Phone number.</param>
        /// <returns>A ResetToken to be used with the OTP.</returns>
        [HttpPost("forgot-password")]
        [Tags("Common Authentication")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand request)
        {
            var result = await _mediator.Send(request);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Step 2: Verify OTP and Reset Password.
        /// </summary>
        /// <remarks>
        /// Takes the ResetToken, the 6-digit OTP, and the new password. If validation passes, updates the user's password securely.
        /// </remarks>
        /// <param name="request">Contains ResetToken, OTP, and the New Password.</param>
        /// <returns>Success status.</returns>
        [HttpPost("reset-password")]
        [Tags("Common Authentication")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
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