using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;
using Wasl.Application.Features.Auth.Commands.RevokeToken;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class RevokeTokenCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly RevokeTokenCommandHandler _handler;
    private readonly ApplicationUser _testUser;
    private readonly RefreshToken _activeRefreshToken;

    public RevokeTokenCommandHandlerTests()
    {
        _testUser = TestDataFactory.CreateTestUser();
        _activeRefreshToken = TestDataFactory.CreateTestRefreshToken(_testUser.Id);
        _testUser.RefreshTokens.Add(_activeRefreshToken);

        _userManagerMock = TestDataFactory.MockUserManager<ApplicationUser>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.InvalidToken"] = "Invalid token.",
            ["Auth.InactiveToken"] = "Inactive token.",
            ["Auth.TokenRevokedSuccessfully"] = "Token revoked."
        });

        var users = new List<ApplicationUser> { _testUser };
        var userDbSetMock = TestDataFactory.MockDbSet(users);
        _userManagerMock.Setup(x => x.Users).Returns(userDbSetMock.Object);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _handler = new RevokeTokenCommandHandler(
            _userManagerMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveToken_RevokesSuccessfully()
    {
        var command = new RevokeTokenCommand { Token = _activeRefreshToken.Token };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeTrue();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ReturnsFailure()
    {
        var command = new RevokeTokenCommand { Token = "nonexistent-token" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Invalid token.");
    }
}
