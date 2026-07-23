using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Auth.Commands.ResetPassword;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _userManagerMock = TestDataFactory.MockUserManager<ApplicationUser>();
        _otpServiceMock = TestDataFactory.MockOtpService();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.UserNotFound"] = "User not found.",
            ["Auth.NewPasswordSameAsOld"] = "New password cannot be same as old.",
            ["Auth.PasswordResetSuccessfully"] = "Password reset successfully."
        });

        var user = TestDataFactory.CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@wasl.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(false);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("reset-token");
        _userManagerMock.Setup(x => x.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _handler = new ResetPasswordCommandHandler(
            _userManagerMock.Object,
            _otpServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidReset_ReturnsSuccess()
    {
        var command = new ResetPasswordCommand
        {
            ResetToken = "valid-reset-token",
            OtpCode = "123456",
            NewPassword = "NewPass@123"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Password reset successfully.");
    }

    [Fact]
    public async Task Handle_InvalidOtp_ReturnsFailure()
    {
        _otpServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Auth.InvalidOTP", null));

        var command = new ResetPasswordCommand
        {
            ResetToken = "invalid-otp",
            OtpCode = "000000",
            NewPassword = "NewPass@123"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SameAsOldPassword_ReturnsFailure()
    {
        _userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), "SamePass@123"))
            .ReturnsAsync(true);

        var command = new ResetPasswordCommand
        {
            ResetToken = "valid-token",
            OtpCode = "123456",
            NewPassword = "SamePass@123"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("New password cannot be same as old.");
    }
}
