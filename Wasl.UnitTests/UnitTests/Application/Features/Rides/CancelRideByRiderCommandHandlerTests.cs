using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Common;
using Wasl.Application.Features.Rides.Commands.CancelRideByRider;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Rides;

public class CancelRideByRiderCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDriverNotificationService> _notificationServiceMock;
    private readonly Mock<IRedisCacheService> _redisCacheMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly CancelRideByRiderCommandHandler _handler;
    private readonly List<Ride> _rides;
    private readonly string _riderId;
    private readonly Ride _testRide;

    public CancelRideByRiderCommandHandlerTests()
    {
        _riderId = "rider-1";
        _testRide = TestDataFactory.CreateTestRide(_riderId, RideStatus.Pending);
        _rides = new List<Ride> { _testRide };

        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserServiceMock = TestDataFactory.MockCurrentUserService(_riderId);
        _notificationServiceMock = new Mock<IDriverNotificationService>();
        _redisCacheMock = new Mock<IRedisCacheService>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.Unauthenticated"] = "User is not authenticated.",
            ["Rides.RideDoesNotExist"] = "Ride does not exist.",
            ["Rides.RideNotYours"] = "This ride does not belong to you.",
            ["Rides.CannotCancelRideAfterStart"] = "Cannot cancel ride after start.",
            ["Rides.AlreadyCancelled"] = "Ride already cancelled.",
            ["Rides.RideCancelledByCustomer"] = "Customer cancelled the ride.",
            ["Rides.CancelledSuccefully"] = "Ride cancelled."
        });

        var ridesDbSetMock = TestDataFactory.MockDbSet(_rides);
        _dbContextMock.Setup(x => x.Rides).Returns(ridesDbSetMock.Object);

        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CancelRideByRiderCommandHandler(
            _dbContextMock.Object,
            _currentUserServiceMock.Object,
            _notificationServiceMock.Object,
            _redisCacheMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_CancelFromPending_ReturnsSuccess()
    {
        var notifiedDrivers = new List<string> { "driver-1", "driver-2" };
        _redisCacheMock.Setup(x => x.GetExcludedDriversForRideAsync(_testRide.Id))
            .ReturnsAsync(notifiedDrivers);

        var command = new CancelRideByRiderCommand { RideId = _testRide.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _testRide.Status.Should().Be(RideStatus.Cancelled);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _redisCacheMock.Verify(x => x.GetExcludedDriversForRideAsync(_testRide.Id), Times.Once);
        _notificationServiceMock.Verify(x => x.HideRideRequestFromDriversAsync(notifiedDrivers, _testRide.Id), Times.Once);
        _redisCacheMock.Verify(x => x.ReleaseRideLockAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CancelFromAccepted_NotifiesDriverAndReleasesLock()
    {
        _testRide.Status = RideStatus.Accepted;
        _testRide.DriverId = "driver-1";

        var command = new CancelRideByRiderCommand { RideId = _testRide.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _testRide.Status.Should().Be(RideStatus.Cancelled);
        _redisCacheMock.Verify(x => x.ReleaseRideLockAsync(_testRide.Id), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyUserRideCancelledAsync("driver-1", It.IsAny<string>()), Times.Once);
        _redisCacheMock.Verify(x => x.GetExcludedDriversForRideAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CancelFromArrived_NotifiesDriverAndReleasesLock()
    {
        _testRide.Status = RideStatus.Arrived;
        _testRide.DriverId = "driver-1";

        var command = new CancelRideByRiderCommand { RideId = _testRide.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _testRide.Status.Should().Be(RideStatus.Cancelled);
        _redisCacheMock.Verify(x => x.ReleaseRideLockAsync(_testRide.Id), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyUserRideCancelledAsync("driver-1", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId()).Returns((string?)null);

        var command = new CancelRideByRiderCommand { RideId = _testRide.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("User is not authenticated.");
    }

    [Fact]
    public async Task Handle_RideNotFound_ReturnsFailure()
    {
        var command = new CancelRideByRiderCommand { RideId = Guid.NewGuid() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride does not exist.");
    }

    [Fact]
    public async Task Handle_RideNotYours_ReturnsFailure()
    {
        _testRide.RiderId = "other-rider";

        var command = new CancelRideByRiderCommand { RideId = _testRide.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("This ride does not belong to you.");
    }

    [Fact]
    public async Task Handle_CannotCancelInProgress_ReturnsFailure()
    {
        _testRide.Status = RideStatus.InProgress;

        var command = new CancelRideByRiderCommand { RideId = _testRide.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Cannot cancel ride after start.");
    }

    [Fact]
    public async Task Handle_CannotCancelCompleted_ReturnsFailure()
    {
        _testRide.Status = RideStatus.Completed;

        var command = new CancelRideByRiderCommand { RideId = _testRide.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Cannot cancel ride after start.");
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ReturnsFailure()
    {
        _testRide.Status = RideStatus.Cancelled;

        var command = new CancelRideByRiderCommand { RideId = _testRide.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride already cancelled.");
    }
}
