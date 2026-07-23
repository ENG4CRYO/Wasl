using FluentAssertions;
using Microsoft.Extensions.Options;
using Wasl.Application.Common.Models;
using Wasl.Application.Services;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Services;

public class RideFareCalculatorTests
{
    private readonly RideFareCalculator _calculator;

    public RideFareCalculatorTests()
    {
        var settings = TestDataFactory.CreateRidePricingSettings();
        _calculator = new RideFareCalculator(settings);
    }

    [Fact]
    public void CalculateFare_CairoToGiza_ReturnsExpectedDistanceAndFare()
    {
        var (fare, distance) = _calculator.CalculateFare(
            30.0444, 31.2357,
            30.0764, 31.2509);

        distance.Should().BeGreaterThan(0);
        fare.Should().BeGreaterThan(0);
        distance.Should().BeApproximately(3.9, 0.5);
    }

    [Fact]
    public void CalculateFare_ShortDistance_AppliesMinimumFare()
    {
        var (fare, distance) = _calculator.CalculateFare(
            30.0444, 31.2357,
            30.0450, 31.2360);

        distance.Should().BeLessThan(0.5);
        fare.Should().Be(10.0m);
    }

    [Fact]
    public void CalculateFare_SameLocation_ReturnsMinimumFare()
    {
        var (fare, distance) = _calculator.CalculateFare(
            30.0444, 31.2357,
            30.0444, 31.2357);

        distance.Should().Be(0);
        fare.Should().Be(10.0m);
    }

    [Fact]
    public void CalculateFare_LongDistance_CalculatesCorrectly()
    {
        var (fare, distance) = _calculator.CalculateFare(
            30.0444, 31.2357,
            31.2001, 29.9187);

        distance.Should().BeGreaterThan(130);
        fare.Should().BeGreaterThan(300);
    }

    [Theory]
    [InlineData(30.0, 31.0, 30.5, 31.5, 73.5)]
    [InlineData(0.0, 0.0, 0.0, 1.0, 111.0)]
    [InlineData(52.5200, 13.4050, 48.8566, 2.3522, 878.0)]
    public void CalculateDistance_KnownPoints_ReturnsApproximatelyExpected(
        double lat1, double lon1, double lat2, double lon2, double expectedKm)
    {
        var (_, distance) = _calculator.CalculateFare(lat1, lon1, lat2, lon2);

        distance.Should().BeApproximately(expectedKm, expectedKm * 0.1);
    }

    [Fact]
    public void CalculateFare_HighBaseFareSettings_UsesCustomValues()
    {
        var highSettings = Options.Create(new RidePricingSettings
        {
            BaseFare = 20.0m,
            PerKmRate = 5.0m,
            PerMinuteRate = 1.0m,
            MinimumFare = 25.0m,
            AverageCitySpeedKmh = 20.0
        });

        var calculator = new RideFareCalculator(highSettings);

        var (fare, distance) = calculator.CalculateFare(
            30.0444, 31.2357,
            30.0764, 31.2509);

        distance.Should().BeGreaterThan(0);
        fare.Should().BeGreaterThan(25.0m);
    }
}
