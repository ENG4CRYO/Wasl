using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Auth.Commands.ForgotPassword;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _userManagerMock = TestDataFactory.MockUserManager<ApplicationUser>();
        _otpServiceMock = TestDataFactory.MockOtpService();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.ForgotPasswordSendOtp"] = "OTP sent for password reset."
        });

        _handler = new ForgotPasswordCommandHandler(
            _userManagerMock.Object,
            _otpServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_InitiatesPasswordReset()
    {
        var user = TestDataFactory.CreateTestUser("user@wasl.com");
        _userManagerMock.Setup(x => x.FindByEmailAsync("user@wasl.com"))
            .ReturnsAsync(user);

        var command = new ForgotPasswordCommand { Email = "user@wasl.com" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        _otpServiceMock.Verify(x => x.InitiatePasswordResetAsync(
            user.Email!, user.FirstName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ReturnsSuccessToPreventEnumeration()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var command = new ForgotPasswordCommand { Email = "nonexistent@wasl.com" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
    }
}
