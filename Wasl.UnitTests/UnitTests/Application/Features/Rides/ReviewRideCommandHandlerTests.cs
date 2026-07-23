using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Common;
using Wasl.Application.Features.Rides.Commands.ReviewRide;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Rides;

public class ReviewRideCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly ReviewRideCommandHandler _handler;
    private readonly List<Ride> _rides;
    private readonly List<RideReview> _rideReviews;
    private readonly List<DriverProfile> _driverProfiles;
    private readonly string _riderId;
    private readonly Ride _testRide;
    private readonly DriverProfile _testDriverProfile;

    public ReviewRideCommandHandlerTests()
    {
        _riderId = "rider-1";
        var driverId = "driver-1";
        _testRide = TestDataFactory.CreateTestRide(_riderId, RideStatus.Completed);
        _testRide.DriverId = driverId;
        _rides = new List<Ride> { _testRide };
        _rideReviews = new List<RideReview>();
        _testDriverProfile = TestDataFactory.CreateTestDriverProfile(driverId, DriverApprovalStatus.Approved);
        _driverProfiles = new List<DriverProfile> { _testDriverProfile };

        _dbContextMock = new Mock<IApplicationDbContext>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["DriverProfiles.Unauthorized"] = "Unauthorized.",
            ["Rides.RideDoesNotExist"] = "Ride does not exist.",
            ["Rides.ReviewNotOwnRide"] = "You cannot review a ride you did not take.",
            ["Rides.ReviewOnlyCompleted"] = "Only completed rides can be reviewed.",
            ["Rides.AlreadyReviewed"] = "Ride already reviewed.",
            ["Rides.ReviewSubmittedSuccessfully"] = "Review submitted."
        });
        _currentUserServiceMock = TestDataFactory.MockCurrentUserService(_riderId);

        var ridesDbSetMock = TestDataFactory.MockDbSet(_rides);
        _dbContextMock.Setup(x => x.Rides).Returns(ridesDbSetMock.Object);

        var rideReviewsDbSetMock = TestDataFactory.MockDbSet(_rideReviews);
        _dbContextMock.Setup(x => x.RideReviews).Returns(rideReviewsDbSetMock.Object);

        var driverProfilesDbSetMock = TestDataFactory.MockDbSet(_driverProfiles);
        _dbContextMock.Setup(x => x.DriverProfiles).Returns(driverProfilesDbSetMock.Object);

        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new ReviewRideCommandHandler(
            _dbContextMock.Object,
            _localizerMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidReview_ReturnsSuccess()
    {
        var rating = 5;
        var command = new ReviewRideCommand
        {
            RideId = _testRide.Id,
            Rating = rating,
            Comment = "Great ride!"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _rideReviews.Should().HaveCount(1);
        _rideReviews[0].RideId.Should().Be(_testRide.Id);
        _rideReviews[0].RiderId.Should().Be(_riderId);
        _rideReviews[0].DriverId.Should().Be(_testRide.DriverId);
        _rideReviews[0].Rating.Should().Be(rating);
        _rideReviews[0].Comment.Should().Be("Great ride!");
        _testDriverProfile.AverageRating.Should().Be(rating);
        _testDriverProfile.TotalReviews.Should().Be(1);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleRidesBySameDriver_CalculatesAverageCorrectly()
    {
        var ride2 = TestDataFactory.CreateTestRide(_riderId, RideStatus.Completed);
        ride2.DriverId = _testRide.DriverId;
        _rides.Add(ride2);

        await _handler.Handle(new ReviewRideCommand { RideId = _testRide.Id, Rating = 4 }, CancellationToken.None);
        await _handler.Handle(new ReviewRideCommand { RideId = ride2.Id, Rating = 2 }, CancellationToken.None);

        _rideReviews.Should().HaveCount(2);
        _testDriverProfile.TotalReviews.Should().Be(2);
        _testDriverProfile.AverageRating.Should().Be(3.0m);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId()).Returns((string?)null);

        var result = await _handler.Handle(
            new ReviewRideCommand { RideId = _testRide.Id, Rating = 4 }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Unauthorized.");
    }

    [Fact]
    public async Task Handle_RideNotFound_ReturnsFailure()
    {
        var result = await _handler.Handle(
            new ReviewRideCommand { RideId = Guid.NewGuid(), Rating = 4 }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride does not exist.");
    }

    [Fact]
    public async Task Handle_NotOwnRide_ReturnsFailure()
    {
        _testRide.RiderId = "other-rider";

        var result = await _handler.Handle(
            new ReviewRideCommand { RideId = _testRide.Id, Rating = 4 }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("You cannot review a ride you did not take.");
    }

    [Fact]
    public async Task Handle_RideNotCompleted_ReturnsFailure()
    {
        _testRide.Status = RideStatus.InProgress;

        var result = await _handler.Handle(
            new ReviewRideCommand { RideId = _testRide.Id, Rating = 4 }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Only completed rides can be reviewed.");
    }

    [Fact]
    public async Task Handle_AlreadyReviewed_ReturnsFailure()
    {
        _rideReviews.Add(new RideReview
        {
            RideId = _testRide.Id,
            RiderId = _riderId,
            Rating = 5
        });

        var result = await _handler.Handle(
            new ReviewRideCommand { RideId = _testRide.Id, Rating = 4 }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Ride already reviewed.");
    }
}
