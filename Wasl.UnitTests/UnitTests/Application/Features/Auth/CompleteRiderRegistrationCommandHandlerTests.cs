using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Wasl.Application.Features.Auth.Commands.RiderRegistration.VerifyRiderRegistration;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Infrastructure.Data;
using Wasl.UnitTests.TestHelpers;
using Xunit;

namespace Wasl.UnitTests.UnitTests.Application.Features.Auth;

public class CompleteRiderRegistrationCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ITokenHelper> _tokenHelperMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _localizerMock;
    private readonly AppDbContext _dbContext;
    private readonly Wasl.Application.Features.Auth.Commands.RiderRegistration.CompleteRiderRegistrationCommandHandler _handler;

    public CompleteRiderRegistrationCommandHandlerTests()
    {
        _userManagerMock = TestDataFactory.MockUserManager<ApplicationUser>();
        _cacheServiceMock = TestDataFactory.MockCacheService();
        _tokenHelperMock = new Mock<ITokenHelper>();
        _localizerMock = TestDataFactory.MockLocalizer<SharedResource>(new Dictionary<string, string>
        {
            ["Auth.RegistrationSessionExpiredOrInvalid"] = "Session expired.",
            ["Auth.EmailAlreadyRegistered"] = "Email already registered.",
            ["Auth.CreateUserFailed"] = "Failed to create user.",
            ["Auth.UserRegisteredSuccessfully"] = "Registration successful."
        });
        _dbContext = TestDataFactory.CreateInMemoryDbContext();

        _cacheServiceMock.Setup(x => x.GetAsync<string>("ValidatedSession:valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test@wasl.com");

        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Rider" });

        _userManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());

        var jwtToken = new JwtSecurityToken();
        _tokenHelperMock.Setup(x => x.CreateJwtToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>(), It.IsAny<IList<System.Security.Claims.Claim>>()))
            .Returns(jwtToken);
        _tokenHelperMock.Setup(x => x.GenerateRefreshToken())
            .Returns(TestDataFactory.CreateTestRefreshToken("user-id"));

        _handler = new Wasl.Application.Features.Auth.Commands.RiderRegistration.CompleteRiderRegistrationCommandHandler(
            _userManagerMock.Object,
            _cacheServiceMock.Object,
            _tokenHelperMock.Object,
            _localizerMock.Object,
            _dbContext);
    }

    [Fact]
    public async Task Handle_ValidRegistration_ReturnsSuccessWithAuthModel()
    {
        var command = new CompleteRiderRegistrationCommand
        {
            RegisterToken = "valid-token",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "07123456789",
            Password = "Test@123"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsAuthenticated.Should().BeTrue();
        result.Data.Email.Should().Be("test@wasl.com");
    }

    [Fact]
    public async Task Handle_ExpiredSession_ReturnsFailure()
    {
        var command = new CompleteRiderRegistrationCommand
        {
            RegisterToken = "invalid-token",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "07123456789",
            Password = "Test@123"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Session expired.");
    }
}
