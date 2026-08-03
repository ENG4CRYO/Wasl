using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Common;
using Wasl.Application.Features.Rides.Commands;
using Wasl.Application.Features.Rides.Commands.AcceptRide;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Rides;

public class AcceptRideCommandHandlerTests
{
    private readonly Mock<IRedisCacheService> _redisCacheMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDriverNotificationService> _driverNotificationMock;
    private readonly AcceptRideCommandHandler _handler;
    private readonly List<Ride> _rides;
    private readonly List<DriverProfile> _driverProfiles;
    private readonly List<ApplicationUser> _users;
    private readonly string _driverId;

    public AcceptRideCommandHandlerTests()
    {
        _driverId = "driver-1";
        _rides = new List<Ride>();
        _driverProfiles = new List<DriverProfile>
        {
            TestDataFactory.CreateTestDriverProfile(_driverId, DriverApprovalStatus.Approved)
        };
        _users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = _driverId,
                FirstName = "Test",
                LastName = "Driver",
                ProfilePictureUrls = "https://example.com/photo.jpg",
                DriverProfile = TestDataFactory.CreateTestDriverProfile(_driverId, DriverApprovalStatus.Approved)
            }
        };

        _redisCacheMock = new Mock<IRedisCacheService>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Driver.ProfileNotFound"] = "Driver profile not found.",
            ["Driver.AccountNotApproved"] = "Account not approved.",
            ["Rides.FailedToAcceptRide"] = "Failed to accept ride.",
            ["Rides.NotFound"] = "Ride not found.",
            ["Rides.AlreadyAccepted"] = "Ride already accepted.",
            ["Rides.RideAcceptanceSucceeded"] = "Ride accepted."
        });
        _currentUserServiceMock = TestDataFactory.MockCurrentUserService(_driverId);
        _driverNotificationMock = new Mock<IDriverNotificationService>();

        var ridesDbSetMock = TestDataFactory.MockDbSet(_rides);
        ridesDbSetMock.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] ids, CancellationToken _) =>
            {
                var id = (Guid)ids[0];
                return _rides.FirstOrDefault(r => r.Id == id);
            });
        _dbContextMock.Setup(x => x.Rides).Returns(ridesDbSetMock.Object);

        var driverProfilesDbSetMock = TestDataFactory.MockDbSet(_driverProfiles);
        _dbContextMock.Setup(x => x.DriverProfiles).Returns(driverProfilesDbSetMock.Object);

        var usersDbSetMock = TestDataFactory.MockDbSet(_users);
        _dbContextMock.Setup(x => x.Users).Returns(usersDbSetMock.Object);

        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _redisCacheMock.Setup(x => x.AcquireRideLockAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _handler = new AcceptRideCommandHandler(
            _redisCacheMock.Object,
            _dbContextMock.Object,
            _localizerMock.Object,
            _currentUserServiceMock.Object,
            _driverNotificationMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        var ride = TestDataFactory.CreateTestRide("rider-1", RideStatus.Pending);
        _rides.Add(ride);

        var command = new AcceptRideCommand { RideId = ride.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        ride.Status.Should().Be(RideStatus.Accepted);
        ride.DriverId.Should().Be(_driverId);

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _driverNotificationMock.Verify(x => x.NotifyRiderRideAcceptedAsync(ride.RiderId, It.Is<DriverRideAcceptedInfoDto>(info =>
            info.DriverId == _driverId &&
            info.DriverName == "Test Driver" &&
            info.DriverProfilePictureUrl == "https://example.com/photo.jpg" &&
            info.VehicleModel == "Toyota Camry" &&
            info.VehicleYear == 2020 &&
            info.VinNumber == "1HGCM82633A004352")), Times.Once);
        _redisCacheMock.Verify(x => x.ReleaseRideLockAsync(ride.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId()).Returns((string?)null);

        var command = new AcceptRideCommand { RideId = Guid.NewGuid().ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_DriverProfileNotFound_ReturnsFailure()
    {
        _driverProfiles.Clear();

        var command = new AcceptRideCommand { RideId = Guid.NewGuid().ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Driver profile not found.");
    }

    [Fact]
    public async Task Handle_DriverNotApproved_ReturnsFailure()
    {
        _driverProfiles.Clear();
        _driverProfiles.Add(TestDataFactory.CreateTestDriverProfile(_driverId, DriverApprovalStatus.Pending));

        var command = new AcceptRideCommand { RideId = Guid.NewGuid().ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Account not approved.");
    }

    [Fact]
    public async Task Handle_LockNotAcquired_ReturnsFailure()
    {
        _redisCacheMock.Setup(x => x.AcquireRideLockAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var command = new AcceptRideCommand { RideId = Guid.NewGuid().ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Failed to accept ride.");
    }

    [Fact]
    public async Task Handle_RideNotFound_ReturnsFailure()
    {
        var command = new AcceptRideCommand { RideId = Guid.NewGuid().ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride not found.");
    }

    [Fact]
    public async Task Handle_RideAlreadyAccepted_ReturnsFailure()
    {
        var ride = TestDataFactory.CreateTestRide("rider-1", RideStatus.Accepted);
        _rides.Add(ride);

        var command = new AcceptRideCommand { RideId = ride.Id.ToString() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride already accepted.");
        _redisCacheMock.Verify(x => x.ReleaseRideLockAsync(ride.Id), Times.Once);
    }
}
