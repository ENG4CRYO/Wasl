using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Auth.Commands.DriverRegistration.InitiateDriverRegistration;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class InitiateDriverRegistrationCommandHandlerTests
{
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly InitiateDriverRegistrationCommandHandler _handler;

    public InitiateDriverRegistrationCommandHandlerTests()
    {
        _otpServiceMock = TestDataFactory.MockOtpService();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.EmailAlreadyRegistered"] = "Email is already registered.",
            ["Auth.RegisterSendOtp"] = "OTP sent successfully."
        });

        _handler = new InitiateDriverRegistrationCommandHandler(
            _otpServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_NewEmail_ReturnsSuccessWithToken()
    {
        var command = new InitiateDriverRegistrationCommand { Email = "newdriver@wasl.com" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ExistingEmail_ReturnsFailure()
    {
        _otpServiceMock.Setup(x => x.InitiateRegistrationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var command = new InitiateDriverRegistrationCommand { Email = "existing@wasl.com" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Email is already registered.");
    }
}
