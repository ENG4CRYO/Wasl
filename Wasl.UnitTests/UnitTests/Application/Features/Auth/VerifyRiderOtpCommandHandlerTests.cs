using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Auth.Commands.RiderRegistration.VerifyRiderOtp;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class VerifyRiderOtpCommandHandlerTests
{
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly VerifyRiderOtpCommandHandler _handler;

    public VerifyRiderOtpCommandHandlerTests()
    {
        _otpServiceMock = TestDataFactory.MockOtpService();
        _cacheServiceMock = TestDataFactory.MockCacheService();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.OtpVerified"] = "OTP verified successfully.",
            ["Auth.SessionExpiredOrInvalidToken"] = "Session expired or invalid.",
            ["Auth.InvalidOTP"] = "Invalid OTP code.",
            ["Auth.MaxOtpAttemptsReached"] = "Maximum OTP attempts reached."
        });

        _handler = new VerifyRiderOtpCommandHandler(
            _otpServiceMock.Object,
            _cacheServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidOtp_ReturnsSuccess()
    {
        var command = new VerifyRiderOtpCommand
        {
            SessionToken = "valid-session",
            OtpCode = "123456"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        result.Message.Should().Be("OTP verified successfully.");
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.Is<string>(s => s.StartsWith("ValidatedSession:")),
            It.IsAny<object>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidOtp_ReturnsFailure()
    {
        _otpServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Auth.InvalidOTP", null));

        var command = new VerifyRiderOtpCommand
        {
            SessionToken = "invalid-session",
            OtpCode = "000000"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Invalid OTP code.");
    }

    [Fact]
    public async Task Handle_ExpiredSession_ReturnsFailure()
    {
        _otpServiceMock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Auth.SessionExpiredOrInvalidToken", null));

        var command = new VerifyRiderOtpCommand
        {
            SessionToken = "expired-session",
            OtpCode = "123456"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Session expired or invalid.");
    }
}
