using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Features.Rides.Queries.EstimateFare;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Rides;

public class EstimateRideFareQueryHandlerTests
{
    private readonly Mock<IRideFareCalculator> _fareCalculatorMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly EstimateRideFareQueryHandler _handler;

    public EstimateRideFareQueryHandlerTests()
    {
        _fareCalculatorMock = new Mock<IRideFareCalculator>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Validation.Rides.PriceCalculated"] = "Price calculated successfully.",
            ["Currency"] = "IQD"
        });

        _handler = new EstimateRideFareQueryHandler(
            _fareCalculatorMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsEstimateDto()
    {
        var fare = 150.0m;
        var distance = 12.5;
        _fareCalculatorMock.Setup(x => x.CalculateFare(30.0, 31.0, 30.5, 31.5))
            .Returns((fare, distance));

        var query = new EstimateRideFareQuery
        {
            PickupLat = 30.0,
            PickupLng = 31.0,
            DropoffLat = 30.5,
            DropoffLng = 31.5
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.EstimatedPrice.Should().Be(fare);
        result.Data.DistanceInKm.Should().Be(distance);
        result.Data.Currency.Should().Be("IQD");
    }

    [Fact]
    public async Task Handle_ZeroDistance_ReturnsMinimumFare()
    {
        var minimumFare = 10.0m;
        _fareCalculatorMock.Setup(x => x.CalculateFare(It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<double>()))
            .Returns((minimumFare, 0.0));

        var query = new EstimateRideFareQuery
        {
            PickupLat = 30.0,
            PickupLng = 31.0,
            DropoffLat = 30.0,
            DropoffLng = 31.0
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.DistanceInKm.Should().Be(0.0);
        result.Data.EstimatedPrice.Should().Be(minimumFare);
    }
}
