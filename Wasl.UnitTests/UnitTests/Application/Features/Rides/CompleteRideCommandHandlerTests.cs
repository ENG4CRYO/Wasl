using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using Wasl.Application.Common;
using Wasl.Application.Common.Models;
using Wasl.Application.Features.Rides.Commands.CompleteRide;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Rides;

public class CompleteRideCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDriverNotificationService> _driverNotificationMock;
    private readonly Mock<IWalletService> _walletServiceMock;
    private readonly Mock<IPaymentGatewayService> _paymentGatewayMock;
    private readonly IOptions<RidePricingSettings> _pricingSettings;
    private readonly CompleteRideCommandHandler _handler;
    private readonly List<Ride> _rides;
    private readonly List<DriverProfile> _driverProfiles;
    private readonly string _driverId;
    private readonly Ride _testRide;

    public CompleteRideCommandHandlerTests()
    {
        _driverId = "driver-1";
        _testRide = TestDataFactory.CreateTestRide("rider-1", RideStatus.InProgress);
        _testRide.DriverId = _driverId;
        _rides = new List<Ride> { _testRide };
        _driverProfiles = new List<DriverProfile>
        {
            TestDataFactory.CreateTestDriverProfile(_driverId, DriverApprovalStatus.Approved)
        };

        _dbContextMock = new Mock<IApplicationDbContext>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["DriverProfiles.NotFound"] = "Driver profile not found.",
            ["DriverProfile.AccountNotApproved"] = "Account not approved.",
            ["Rides.RideDoesNotExist"] = "Ride does not exist.",
            ["Rides.RideNotYours"] = "This ride does not belong to you.",
            ["Rides.StatusNotInProgress"] = "Ride status is not InProgress.",
            ["Rides.StatusAlreadyCompleted"] = "Ride already completed.",
            ["Rides.RideCompletedSuccessfully"] = "Ride completed.",
            ["Rides.CardPaymentFailed"] = "Payment failed.",
            ["Rides.PaymentTokenRequired"] = "Payment token required."
        });
        _currentUserServiceMock = TestDataFactory.MockCurrentUserService(_driverId);
        _driverNotificationMock = new Mock<IDriverNotificationService>();
        _walletServiceMock = new Mock<IWalletService>();
        _paymentGatewayMock = new Mock<IPaymentGatewayService>();
        _pricingSettings = TestDataFactory.CreateRidePricingSettings();

        var ridesDbSetMock = TestDataFactory.MockDbSet(_rides);
        _dbContextMock.Setup(x => x.Rides).Returns(ridesDbSetMock.Object);

        var driverProfilesDbSetMock = TestDataFactory.MockDbSet(_driverProfiles);
        _dbContextMock.Setup(x => x.DriverProfiles).Returns(driverProfilesDbSetMock.Object);

        var transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        _dbContextMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _walletServiceMock
            .Setup(x => x.TransferFundsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(),
                It.IsAny<TransactionType>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletOperationResult(true, 100));

        _walletServiceMock
            .Setup(x => x.DeductFundsAsync(It.IsAny<string>(), It.IsAny<decimal>(),
                It.IsAny<TransactionType>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletOperationResult(true, 100));

        _walletServiceMock
            .Setup(x => x.AddFundsAsync(It.IsAny<string>(), It.IsAny<decimal>(),
                It.IsAny<TransactionType>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletOperationResult(true, 100));

        _handler = new CompleteRideCommandHandler(
            _dbContextMock.Object,
            _localizerMock.Object,
            _currentUserServiceMock.Object,
            _driverNotificationMock.Object,
            _walletServiceMock.Object,
            _paymentGatewayMock.Object,
            _pricingSettings);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        var command = new CompleteRideCommand { RideId = _testRide.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _testRide.Status.Should().Be(RideStatus.Completed);
        _testRide.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _driverNotificationMock.Verify(x => x.NotifyRiderRideCompletedAsync(_testRide.RiderId, _testRide.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId()).Returns((string?)null);

        var command = new CompleteRideCommand { RideId = _testRide.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_DriverProfileNotFound_ReturnsFailure()
    {
        _driverProfiles.Clear();

        var command = new CompleteRideCommand { RideId = _testRide.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Driver profile not found.");
    }

    [Fact]
    public async Task Handle_DriverNotApproved_ReturnsFailure()
    {
        _driverProfiles.Clear();
        _driverProfiles.Add(TestDataFactory.CreateTestDriverProfile(_driverId, DriverApprovalStatus.Pending));

        var command = new CompleteRideCommand { RideId = _testRide.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Account not approved.");
    }

    [Fact]
    public async Task Handle_RideNotFound_ReturnsFailure()
    {
        var command = new CompleteRideCommand { RideId = Guid.NewGuid().ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride does not exist.");
    }

    [Fact]
    public async Task Handle_RideNotYours_ReturnsFailure()
    {
        var otherRide = TestDataFactory.CreateTestRide("rider-2", RideStatus.InProgress);
        otherRide.DriverId = "other-driver";
        _rides.Add(otherRide);

        var command = new CompleteRideCommand { RideId = otherRide.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("This ride does not belong to you.");
    }

    [Fact]
    public async Task Handle_WrongStatus_ReturnsFailure()
    {
        _testRide.Status = RideStatus.Pending;

        var command = new CompleteRideCommand { RideId = _testRide.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride status is not InProgress.");
    }

    [Fact]
    public async Task Handle_AlreadyCompleted_ReturnsStatusNotInProgress()
    {
        _testRide.Status = RideStatus.Completed;

        var command = new CompleteRideCommand { RideId = _testRide.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride status is not InProgress.");
    }
}
