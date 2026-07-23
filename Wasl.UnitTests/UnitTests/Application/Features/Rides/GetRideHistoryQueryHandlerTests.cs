using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Features.Rides.Queries.GetRideHistory;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Rides;

public class GetRideHistoryQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetRideHistoryQueryHandler _handler;
    private readonly List<Ride> _rides;
    private const string UserId = "user-1";

    public GetRideHistoryQueryHandlerTests()
    {
        _rides = new List<Ride>();

        _dbContextMock = new Mock<IApplicationDbContext>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Rides.Completed"] = "Completed",
            ["Rides.Cancelled"] = "Cancelled",
            ["Rides.HistoryRetrievedSuccessfully"] = "Ride history retrieved successfully."
        });
        _currentUserServiceMock = TestDataFactory.MockCurrentUserService(UserId);

        var ridesDbSetMock = TestDataFactory.MockDbSet(_rides);
        _dbContextMock.Setup(x => x.Rides).Returns(ridesDbSetMock.Object);

        _handler = new GetRideHistoryQueryHandler(
            _dbContextMock.Object,
            _currentUserServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_RiderHasCompletedAndCancelledRides_ReturnsBoth()
    {
        _rides.Add(CreateRide(UserId, null, RideStatus.Completed));
        _rides.Add(CreateRide(UserId, null, RideStatus.Cancelled));

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_RiderHasPendingRides_FiltersThemOut()
    {
        _rides.Add(CreateRide(UserId, null, RideStatus.Pending));
        _rides.Add(CreateRide(UserId, null, RideStatus.Accepted));
        _rides.Add(CreateRide(UserId, null, RideStatus.InProgress));

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RidesBelongToDifferentUser_ReturnsEmpty()
    {
        _rides.Add(CreateRide("other-user", null, RideStatus.Completed));
        _rides.Add(CreateRide("other-user", null, RideStatus.Cancelled));

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DriverHasCompletedAndCancelledRides_ReturnsBoth()
    {
        _rides.Add(CreateRide("rider-1", UserId, RideStatus.Completed));
        _rides.Add(CreateRide("rider-2", UserId, RideStatus.Cancelled));

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoMatchingRides_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        for (int i = 0; i < 15; i++)
        {
            _rides.Add(CreateRide(UserId, null, RideStatus.Completed));
        }

        var result = await _handler.Handle(new GetRideHistoryQuery { PageNumber = 2, PageSize = 5 }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(15);
        result.Data.CurrentPage.Should().Be(2);
        result.Data.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_StatusLocalization_CompletedRideReturnsLocalizedStatus()
    {
        _rides.Add(CreateRide(UserId, null, RideStatus.Completed));

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Data!.Items[0].Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_StatusLocalization_CancelledRideReturnsLocalizedStatus()
    {
        _rides.Add(CreateRide(UserId, null, RideStatus.Cancelled));

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Data!.Items[0].Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Handle_ReturnsRequestedDateAndTime()
    {
        var ride = CreateRide(UserId, null, RideStatus.Completed);
        ride.RequestedAt = new DateTime(2026, 7, 22, 14, 30, 0);
        _rides.Add(ride);

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        var dto = result.Data!.Items[0];
        dto.RequestedDate.Should().Be(new DateTime(2026, 7, 22));
        dto.RequestedTime.Should().Be(new TimeSpan(14, 30, 0));
    }

    [Fact]
    public async Task Handle_ReturnsCorrectPrice()
    {
        _rides.Add(CreateRide(UserId, null, RideStatus.Completed));

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Data!.Items[0].Price.Should().Be(50.0m);
    }

    [Fact]
    public async Task Handle_ReturnsPagedListWithCorrectType()
    {
        _rides.Add(CreateRide(UserId, null, RideStatus.Completed));

        var result = await _handler.Handle(new GetRideHistoryQuery(), CancellationToken.None);

        result.Data.Should().BeOfType<PagedList<RideHistoryDto>>();
    }

    private static Ride CreateRide(string? riderId, string? driverId, RideStatus status)
    {
        var ride = TestDataFactory.CreateTestRide(riderId ?? string.Empty, status);
        ride.DriverId = driverId;
        return ride;
    }
}
