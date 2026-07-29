using FluentAssertions;
using Moq;
using Wasl.Application.Common;
using Wasl.Application.Features.DriverEarnings.Queries.GetOverview;
using Wasl.Application.Interfaces.Common;
using Wasl.Core.Entities;
using Wasl.Core.Enums;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.DriverEarnings;

public class GetDriverEarningsOverviewQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetDriverEarningsOverviewQueryHandler _handler;
    private readonly List<Ride> _rides;
    private readonly List<DriverOnlineLog> _onlineLogs;
    private const string DriverId = "driver-1";
    private static readonly DateTime StartDate = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndDate = new(2026, 7, 29, 23, 59, 59, DateTimeKind.Utc);

    public GetDriverEarningsOverviewQueryHandlerTests()
    {
        _rides = new List<Ride>();
        _onlineLogs = new List<DriverOnlineLog>();

        _dbContextMock = new Mock<IApplicationDbContext>();

        var ridesDbSetMock = TestDataFactory.MockDbSet(_rides);
        _dbContextMock.Setup(x => x.Rides).Returns(ridesDbSetMock.Object);

        var logsDbSetMock = TestDataFactory.MockDbSet(_onlineLogs);
        _dbContextMock.Setup(x => x.DriverOnlineLogs).Returns(logsDbSetMock.Object);

        _handler = new GetDriverEarningsOverviewQueryHandler(_dbContextMock.Object);
    }

    private static GetDriverEarningsOverviewQuery CreateQuery() => new()
    {
        DriverId = DriverId,
        StartDate = StartDate,
        EndDate = EndDate
    };

    private static Ride CreateCompletedRide(string driverId, decimal earnings, DateTime completedAt)
    {
        var ride = TestDataFactory.CreateTestRide("rider-1", RideStatus.Completed);
        ride.DriverId = driverId;
        ride.DriverNetEarnings = earnings;
        ride.CompletedAt = completedAt;
        return ride;
    }

    private static DriverOnlineLog CreateOnlineLog(string driverId, int minutes, DateTime start, DateTime end) => new()
    {
        Id = Guid.NewGuid(),
        DriverId = driverId,
        StartTime = start,
        EndTime = end,
        DurationMinutes = minutes
    };

    [Fact]
    public async Task Handle_ValidRequest_ReturnsAggregatedOverview()
    {
        _rides.Add(CreateCompletedRide(DriverId, 50.0m, new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc)));
        _rides.Add(CreateCompletedRide(DriverId, 75.0m, new DateTime(2026, 7, 15, 14, 30, 0, DateTimeKind.Utc)));
        _rides.Add(CreateCompletedRide(DriverId, 100.0m, new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc)));

        _onlineLogs.Add(CreateOnlineLog(DriverId, 60,
            new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc)));
        _onlineLogs.Add(CreateOnlineLog(DriverId, 120,
            new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 15, 14, 0, 0, DateTimeKind.Utc)));

        var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CompletedRides.Should().Be(3);
        result.Data.TotalEarnings.Should().Be(225.0m);
        result.Data.OnlineMinutes.Should().Be(180);
        result.Data.CanCashOut.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullRideStats_ReturnsZeroValues()
    {
        _rides.Add(CreateCompletedRide(DriverId, 50.0m, new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc)));

        var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.CompletedRides.Should().Be(0);
        result.Data.TotalEarnings.Should().Be(0);
        result.Data.OnlineMinutes.Should().Be(0);
        result.Data.CanCashOut.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_RidesOutsideDateRange_ExcludedFromAggregation()
    {
        _rides.Add(CreateCompletedRide(DriverId, 50.0m, new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc)));
        _rides.Add(CreateCompletedRide(DriverId, 75.0m, new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc)));

        var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        result.Data!.CompletedRides.Should().Be(0);
        result.Data.TotalEarnings.Should().Be(0);
        result.Data.CanCashOut.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoMatchingOnlineLogs_ReturnsZeroMinutes()
    {
        _rides.Add(CreateCompletedRide(DriverId, 80.0m, new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc)));

        _onlineLogs.Add(CreateOnlineLog(DriverId, 60,
            new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc)));

        var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.CompletedRides.Should().Be(1);
        result.Data.TotalEarnings.Should().Be(80.0m);
        result.Data.OnlineMinutes.Should().Be(0);
        result.Data.CanCashOut.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoData_ReturnsZeroForAll()
    {
        var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.CompletedRides.Should().Be(0);
        result.Data.TotalEarnings.Should().Be(0);
        result.Data.OnlineMinutes.Should().Be(0);
        result.Data.CanCashOut.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OtherDriverDataExcluded_ReturnsZero()
    {
        _rides.Add(CreateCompletedRide("other-driver", 200.0m, new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc)));

        _onlineLogs.Add(CreateOnlineLog("other-driver", 300,
            new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 1, 13, 0, 0, DateTimeKind.Utc)));

        var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        result.Data!.CompletedRides.Should().Be(0);
        result.Data.TotalEarnings.Should().Be(0);
        result.Data.OnlineMinutes.Should().Be(0);
        result.Data.CanCashOut.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ZeroEarnings_SetsCanCashOutFalse()
    {
        _rides.Add(CreateCompletedRide(DriverId, 0.0m, new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc)));

        var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        result.Data!.CompletedRides.Should().Be(1);
        result.Data.TotalEarnings.Should().Be(0);
        result.Data.CanCashOut.Should().BeFalse();
    }
}
