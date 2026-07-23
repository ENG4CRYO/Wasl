using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Auth.Commands.RiderRegistration.InitiateRiderRegistration;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class InitiateRiderRegistrationCommandHandlerTests
{
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly InitiateRiderRegistrationCommandHandler _handler;

    public InitiateRiderRegistrationCommandHandlerTests()
    {
        _otpServiceMock = TestDataFactory.MockOtpService();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.EmailAlreadyRegistered"] = "Email is already registered.",
            ["Auth.RegisterSendOtp"] = "OTP sent successfully."
        });

        _handler = new InitiateRiderRegistrationCommandHandler(
            _otpServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_NewEmail_ReturnsSuccessWithToken()
    {
        var command = new InitiateRiderRegistrationCommand { Email = "new@wasl.com" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        result.Message.Should().Be("OTP sent successfully.");
    }

    [Fact]
    public async Task Handle_ExistingEmail_ReturnsFailure()
    {
        _otpServiceMock.Setup(x => x.InitiateRegistrationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var command = new InitiateRiderRegistrationCommand { Email = "existing@wasl.com" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Email is already registered.");
    }
}
