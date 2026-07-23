using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Wasl.Application.Features.Auth.Commands.RefreshToken;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenHelper> _tokenHelperMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly RefreshTokenCommandHandler _handler;
    private readonly ApplicationUser _testUser;
    private readonly RefreshToken _activeRefreshToken;

    public RefreshTokenCommandHandlerTests()
    {
        _testUser = TestDataFactory.CreateTestUser();
        _activeRefreshToken = TestDataFactory.CreateTestRefreshToken(_testUser.Id);
        _testUser.RefreshTokens.Add(_activeRefreshToken);

        _userManagerMock = TestDataFactory.MockUserManager<ApplicationUser>();
        _tokenHelperMock = new Mock<ITokenHelper>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.InvalidToken"] = "Invalid token.",
            ["Auth.InactiveToken"] = "Inactive token.",
            ["Auth.TokenRefreshedSuccessfully."] = "Token refreshed."
        });

        var users = new List<ApplicationUser> { _testUser };
        var userDbSetMock = TestDataFactory.MockDbSet(users);
        _userManagerMock.Setup(x => x.Users).Returns(userDbSetMock.Object);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Rider" });
        _userManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var jwtToken = new JwtSecurityToken();
        _tokenHelperMock.Setup(x => x.CreateJwtToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>(), It.IsAny<IList<System.Security.Claims.Claim>>()))
            .Returns(jwtToken);
        _tokenHelperMock.Setup(x => x.GenerateRefreshToken())
            .Returns(TestDataFactory.CreateTestRefreshToken(_testUser.Id));

        _handler = new RefreshTokenCommandHandler(
            _userManagerMock.Object,
            _tokenHelperMock.Object,
            _localizerMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveToken_ReturnsNewAuthModel()
    {
        var command = new RefreshTokenCommand { Token = _activeRefreshToken.Token };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsAuthenticated.Should().BeTrue();
        result.Data.Token.Should().NotBeNullOrEmpty();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ReturnsFailure()
    {
        var command = new RefreshTokenCommand { Token = "nonexistent-token" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Invalid token.");
    }
}
