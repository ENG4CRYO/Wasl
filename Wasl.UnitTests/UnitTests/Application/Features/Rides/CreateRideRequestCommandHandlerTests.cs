using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Common;
using Wasl.Application.Features.Rides.Commands.RequestRide;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Rides;

public class CreateRideRequestCommandHandlerTests
{
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<IRideDispatchService> _dispatchServiceMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IRideFareCalculator> _priceCalculatorMock;
    private readonly Mock<IWalletService> _walletServiceMock;
    private readonly CreateRideRequestCommandHandler _handler;
    private readonly List<Ride> _rides;
    private readonly List<ApplicationUser> _users;

    public CreateRideRequestCommandHandlerTests()
    {
        _rides = new List<Ride>();
        _users = new List<ApplicationUser>
        {
            TestDataFactory.CreateTestUser("rider@test.com", "test-user-id")
        };

        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _dispatchServiceMock = new Mock<IRideDispatchService>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.Unauthenticated"] = "User is not authenticated.",
            ["Rides.RequestRideReceivedSuccessfully"] = "Ride request received.",
            ["Rides.InsufficientWalletBalance"] = "Insufficient wallet balance."
        });
        _currentUserServiceMock = TestDataFactory.MockCurrentUserService();
        _priceCalculatorMock = new Mock<IRideFareCalculator>();
        _walletServiceMock = new Mock<IWalletService>();

        var ridesDbSetMock = TestDataFactory.MockDbSet(_rides);
        _dbContextMock.Setup(x => x.Rides).Returns(ridesDbSetMock.Object);

        var usersDbSetMock = TestDataFactory.MockDbSet(_users);
        _dbContextMock.Setup(x => x.Users).Returns(usersDbSetMock.Object);

        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _priceCalculatorMock.Setup(x => x.CalculateFare(It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<double>()))
            .Returns((50.0m, 10.0));

        _handler = new CreateRideRequestCommandHandler(
            _backgroundJobClientMock.Object,
            _dispatchServiceMock.Object,
            _dbContextMock.Object,
            _localizerMock.Object,
            _currentUserServiceMock.Object,
            _priceCalculatorMock.Object,
            _walletServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithNewRideId()
    {
        var command = new CreateRideRequestCommand
        {
            pickupLat = 30.0444,
            pickupLng = 31.2357,
            dropoffLat = 30.0764,
            dropoffLng = 31.2509
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        _rides.Should().HaveCount(1);
        _rides[0].RiderId.Should().Be("test-user-id");
        _rides[0].Status.Should().Be(RideStatus.Pending);
        _rides[0].CalculatedPrice.Should().Be(50.0m);

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId()).Returns((string?)null);

        var command = new CreateRideRequestCommand
        {
            pickupLat = 30.0444,
            pickupLng = 31.2357,
            dropoffLat = 30.0764,
            dropoffLng = 31.2509
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeEmpty();
        _rides.Should().BeEmpty();
    }
}
