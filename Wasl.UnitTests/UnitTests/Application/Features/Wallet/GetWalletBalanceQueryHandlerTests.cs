using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Wallet.Queries.GetDriverWalletBalance;
using Wasl.Application.Features.Wallet.Queries.GetRiderWalletBalance;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Wallet;

public class GetWalletBalanceQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly List<Wasl.Core.Entities.ApplicationUser> _users;
    private readonly GetDriverWalletBalanceQueryHandler _driverHandler;
    private readonly GetRiderWalletBalanceQueryHandler _riderHandler;
    private const string UserId = "user-1";

    public GetWalletBalanceQueryHandlerTests()
    {
        _users = new List<Wasl.Core.Entities.ApplicationUser>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        var usersDbSetMock = TestDataFactory.MockDbSet(_users);
        _dbContextMock.Setup(x => x.Users).Returns(usersDbSetMock.Object);

        _currentUserServiceMock = TestDataFactory.MockCurrentUserService(UserId);
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.Unauthenticated"] = "Unauthenticated",
            ["Auth.UserNotFound"] = "User not found",
            ["Wallet.BalanceRetrievedSuccessfully"] = "Wallet balance retrieved successfully."
        });

        _driverHandler = new GetDriverWalletBalanceQueryHandler(
            _dbContextMock.Object,
            _currentUserServiceMock.Object,
            _localizerMock.Object);

        _riderHandler = new GetRiderWalletBalanceQueryHandler(
            _dbContextMock.Object,
            _currentUserServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task DriverHandle_ValidUser_ReturnsBalance()
    {
        var user = TestDataFactory.CreateTestUser("driver@wasl.com", UserId);
        user.Balance = 2050m;
        _users.Add(user);

        var result = await _driverHandler.Handle(new GetDriverWalletBalanceQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Balance.Should().Be(2050m);
    }

    [Fact]
    public async Task RiderHandle_ValidUser_ReturnsBalance()
    {
        var user = TestDataFactory.CreateTestUser("rider@wasl.com", UserId);
        user.Balance = 5000m;
        _users.Add(user);

        var result = await _riderHandler.Handle(new GetRiderWalletBalanceQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Balance.Should().Be(5000m);
    }

    [Fact]
    public async Task DriverHandle_Unauthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId()).Returns((string?)null);

        var result = await _driverHandler.Handle(new GetDriverWalletBalanceQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Unauthenticated");
    }

    [Fact]
    public async Task RiderHandle_Unauthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId()).Returns((string?)null);

        var result = await _riderHandler.Handle(new GetRiderWalletBalanceQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Unauthenticated");
    }

    [Fact]
    public async Task DriverHandle_UserNotFound_ReturnsFailure()
    {
        var result = await _driverHandler.Handle(new GetDriverWalletBalanceQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task RiderHandle_UserNotFound_ReturnsFailure()
    {
        var result = await _riderHandler.Handle(new GetRiderWalletBalanceQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("User not found");
    }
}
