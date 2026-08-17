using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Profile.Commands.UpdateDriverProfile;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Profile;

public class UpdateDriverProfileCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly List<ApplicationUser> _users;
    private readonly UpdateDriverProfileCommandHandler _handler;
    private const string UserId = "user-1";

    public UpdateDriverProfileCommandHandlerTests()
    {
        _users = new List<ApplicationUser>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        var usersDbSetMock = TestDataFactory.MockDbSet(_users);
        _dbContextMock.Setup(x => x.Users).Returns(usersDbSetMock.Object);

        _currentUserServiceMock = TestDataFactory.MockCurrentUserService(UserId);
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.Unauthenticated"] = "Unauthenticated",
            ["Auth.UserNotFound"] = "User not found",
            ["DriverProfiles.NotFound"] = "The driver file was not found",
            ["Profile.PhoneNumberAlreadyTaken"] = "Phone number is already in use",
            ["Profile.UpdatedSuccessfully"] = "Profile updated successfully"
        });

        _handler = new UpdateDriverProfileCommandHandler(
            _dbContextMock.Object,
            _currentUserServiceMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesProfileAndReturnsDto()
    {
        var user = TestDataFactory.CreateTestUser("driver@wasl.com", UserId);
        user.PhoneNumber = "07701234567";
        user.City = "Baghdad";
        user.Address = "Main Street";
        user.Balance = 1000m;
        user.DriverProfile = TestDataFactory.CreateTestDriverProfile(UserId);
        user.DriverProfile.AverageRating = 4.5m;
        user.DriverProfile.TotalReviews = 10;
        _users.Add(user);

        var command = new UpdateDriverProfileCommand
        {
            FirstName = "Ali",
            LastName = "Hassan",
            PhoneNumber = "07709876543"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FirstName.Should().Be("Ali");
        result.Data.LastName.Should().Be("Hassan");
        result.Data.PhoneNumber.Should().Be("07709876543");
        result.Data.Email.Should().Be("driver@wasl.com");
        result.Data.City.Should().Be("Baghdad");
        result.Data.Address.Should().Be("Main Street");
        result.Data.AverageRating.Should().Be(4.5m);
        result.Data.TotalReviews.Should().Be(10);
        result.Data.Balance.Should().Be(1000m);
        user.FirstName.Should().Be("Ali");
        user.LastName.Should().Be("Hassan");
        user.PhoneNumber.Should().Be("07709876543");
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId()).Returns((string?)null);

        var result = await _handler.Handle(new UpdateDriverProfileCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Unauthenticated");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        var result = await _handler.Handle(new UpdateDriverProfileCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_DriverProfileNotFound_ReturnsFailure()
    {
        var user = TestDataFactory.CreateTestUser("driver@wasl.com", UserId);
        _users.Add(user);

        var result = await _handler.Handle(new UpdateDriverProfileCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("The driver file was not found");
    }

    [Fact]
    public async Task Handle_PhoneNumberAlreadyTaken_ReturnsFailure()
    {
        var user = TestDataFactory.CreateTestUser("driver@wasl.com", UserId);
        user.PhoneNumber = "07701234567";
        user.DriverProfile = TestDataFactory.CreateTestDriverProfile(UserId);
        _users.Add(user);

        var otherUser = TestDataFactory.CreateTestUser("other@wasl.com", "user-2");
        otherUser.PhoneNumber = "07709876543";
        _users.Add(otherUser);

        var command = new UpdateDriverProfileCommand
        {
            FirstName = "Ali",
            LastName = "Hassan",
            PhoneNumber = "07709876543"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Phone number is already in use");
    }
}