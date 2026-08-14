using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Auth.Commands.ResetPassword;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _userManagerMock = TestDataFactory.MockUserManager<ApplicationUser>();
        _cacheServiceMock = TestDataFactory.MockCacheService();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.PasswordResetSessionExpiredOrInvalid"] = "Password reset session expired or invalid.",
            ["Auth.UserNotFound"] = "User not found.",
            ["Auth.NewPasswordSameAsOld"] = "New password cannot be same as old.",
            ["Auth.PasswordResetSuccessfully"] = "Password reset successfully.",
            ["Auth.ResetPasswordFailed"] = "Failed to reset password."
        });

        _cacheServiceMock.Setup(x => x.GetAsync<string>($"ValidatedResetSession:valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test@wasl.com");

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
            _cacheServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsSuccess()
    {
        var command = new ResetPasswordCommand
        {
            Token = "valid-token",
            NewPassword = "NewPass@123"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Password reset successfully.");
        _cacheServiceMock.Verify(x => x.RemoveAsync("ValidatedResetSession:valid-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidSession_ReturnsFailure()
    {
        _cacheServiceMock.Setup(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var command = new ResetPasswordCommand
        {
            Token = "expired-token",
            NewPassword = "NewPass@123"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Password reset session expired or invalid.");
    }

    [Fact]
    public async Task Handle_SameAsOldPassword_ReturnsFailure()
    {
        _userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), "SamePass@123"))
            .ReturnsAsync(true);

        var command = new ResetPasswordCommand
        {
            Token = "valid-token",
            NewPassword = "SamePass@123"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("New password cannot be same as old.");
    }
}