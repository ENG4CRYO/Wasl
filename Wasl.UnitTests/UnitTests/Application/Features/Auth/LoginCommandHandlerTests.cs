using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Features.Auth.Commands.Login;
using Wasl.Application.Helpers;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenHelper> _tokenHelperMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly IOptions<JWT> _jwtOptions;
    private readonly LoginCommandHandler _handler;

    private readonly ApplicationUser _testUser;
    private readonly List<RefreshToken> _refreshTokens;

    public LoginCommandHandlerTests()
    {
        _testUser = TestDataFactory.CreateTestUser();
        _refreshTokens = new List<RefreshToken>();

        _userManagerMock = TestDataFactory.MockUserManager<ApplicationUser>();
        _tokenHelperMock = new Mock<ITokenHelper>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.InvalidCredentials"] = "Invalid email or password."
        });
        _jwtOptions = TestDataFactory.CreateJwtOptions();

        var userDbSetMock = TestDataFactory.MockDbSet(new List<ApplicationUser> { _testUser });
        _userManagerMock.Setup(x => x.Users).Returns(userDbSetMock.Object);

        var refreshTokenDbSetMock = TestDataFactory.MockDbSet(_refreshTokens);
        _dbContextMock.Setup(x => x.RefreshTokens).Returns(refreshTokenDbSetMock.Object);

        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Rider" });

        _userManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var jwtToken = new JwtSecurityToken(
            issuer: _jwtOptions.Value.Issuer,
            audience: _jwtOptions.Value.Audience,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.Value.AccessTokenValidityInMinutes));

        _tokenHelperMock.Setup(x => x.CreateJwtToken(
                It.IsAny<ApplicationUser>(),
                It.IsAny<IList<string>>(),
                It.IsAny<IList<System.Security.Claims.Claim>>()))
            .Returns(jwtToken);

        _tokenHelperMock.Setup(x => x.GenerateRefreshToken())
            .Returns(TestDataFactory.CreateTestRefreshToken(_testUser.Id));

        _handler = new LoginCommandHandler(
            _userManagerMock.Object,
            _tokenHelperMock.Object,
            _jwtOptions,
            _localizerMock.Object,
            _dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithAuthModel()
    {
        var command = TestDataFactory.CreateValidLoginCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsAuthenticated.Should().BeTrue();
        result.Data.Email.Should().Be(_testUser.Email);
        result.Data.Token.Should().NotBeNullOrEmpty();
        result.Data.Roles.Should().Contain("Rider");

        _tokenHelperMock.Verify(x => x.ManageUserSessions(_testUser), Times.Once);
        _tokenHelperMock.Verify(x => x.CreateJwtToken(_testUser, It.IsAny<IList<string>>(), It.IsAny<IList<System.Security.Claims.Claim>>()), Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsFailure()
    {
        _userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var command = TestDataFactory.CreateValidLoginCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        var emptyUserDbSetMock = TestDataFactory.MockDbSet(new List<ApplicationUser>());
        _userManagerMock.Setup(x => x.Users).Returns(emptyUserDbSetMock.Object);

        var command = TestDataFactory.CreateValidLoginCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Invalid email or password.");
    }
}
