using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Wasl.Application.Common.Models;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Features.Auth.Commands.Login;
using Wasl.Application.Helpers;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Interfaces.Services;
using Wasl.Infrastructure.Data;
using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;
using Wasl.Core.Enums;

namespace Wasl.UnitTests.TestHelpers
{
    public static class TestDataFactory
    {
        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            var hasher = new Mock<IPasswordHasher<TUser>>();
            var userValidators = new List<IUserValidator<TUser>>();
            var passwordValidators = new List<IPasswordValidator<TUser>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<TUser>>>();

            return new Mock<UserManager<TUser>>(
                store.Object, options.Object, hasher.Object,
                userValidators, passwordValidators, keyNormalizer.Object,
                errors.Object, services.Object, logger.Object);
        }

        public static Mock<DbSet<T>> MockDbSet<T>(List<T> data) where T : class
        {
            var mock = new Mock<DbSet<T>>();
            var asyncProvider = TestAsyncQueryProvider.Instance;

            mock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

            mock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(asyncProvider);

            mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(Expression.Constant(data.AsQueryable()));
            mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(typeof(T));
            mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

            mock.Setup(x => x.Add(It.IsAny<T>()))
                .Callback<T>(entity => data.Add(entity))
                .Returns(default(EntityEntry<T>)!);

            mock.Setup(x => x.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Returns<T, CancellationToken>((entity, ct) =>
                {
                    data.Add(entity);
                    return new ValueTask<EntityEntry<T>>(default(EntityEntry<T>)!);
                });

            return mock;
        }

        public static Mock<IStringLocalizer<T>> MockLocalizer<T>(Dictionary<string, string> translations) where T : class
        {
            var mock = new Mock<IStringLocalizer<T>>();
            mock.Setup(x => x[It.IsAny<string>()])
                .Returns<string>(key => new LocalizedString(key, translations.GetValueOrDefault(key, key)));
            return mock;
        }

        public static IOptions<JWT> CreateJwtOptions()
        {
            return Options.Create(new JWT
            {
                Key = "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
                Issuer = "WaslTest",
                Audience = "WaslTestUsers",
                AccessTokenValidityInMinutes = 15,
                RefreshTokenValidityInDays = 7
            });
        }

        public static IOptions<RidePricingSettings> CreateRidePricingSettings()
        {
            return Options.Create(new RidePricingSettings
            {
                BaseFare = 5.0m,
                PerKmRate = 2.5m,
                PerMinuteRate = 0.5m,
                MinimumFare = 10.0m,
                AverageCitySpeedKmh = 30.0
            });
        }

        public static ApplicationUser CreateTestUser(string? email = null, string? userId = null)
        {
            return new ApplicationUser
            {
                Id = userId ?? Guid.NewGuid().ToString(),
                UserName = email ?? "test@wasl.com",
                Email = email ?? "test@wasl.com",
                FirstName = "Test",
                LastName = "User",
                Balance = 0,
                IsOnline = false
            };
        }

        public static RefreshToken CreateTestRefreshToken(string userId, bool isExpired = false, bool isRevoked = false)
        {
            return new RefreshToken
            {
                Id = 1,
                Token = Guid.NewGuid().ToString(),
                Expires = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                UserId = userId
            };
        }

        public static LoginCommand CreateValidLoginCommand()
        {
            return new LoginCommand
            {
                Email = "test@wasl.com",
                Password = "Test@123"
            };
        }

        public static Ride CreateTestRide(string riderId, RideStatus status = RideStatus.Pending)
        {
            return new Ride
            {
                Id = Guid.NewGuid(),
                RiderId = riderId,
                PickupLatitude = 30.0444,
                PickupLongitude = 31.2357,
                DropoffLatitude = 30.0764,
                DropoffLongitude = 31.2509,
                CalculatedPrice = 50.0m,
                PaymentMethod = PaymentMethod.Cash,
                Status = status,
                RequestedAt = DateTime.UtcNow
            };
        }

        public static Mock<ICurrentUserService> MockCurrentUserService(string? userId = null)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(x => x.UserId()).Returns(userId ?? "test-user-id");
            return mock;
        }

        public static AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"WaslTestDb_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var currentUserMock = MockCurrentUserService();

            return new AppDbContext(options, currentUserMock.Object);
        }

        public static Mock<ICacheService> MockCacheService()
        {
            var mock = new Mock<ICacheService>();
            mock.Setup(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
            mock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mock.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        public static Mock<IOtpService> MockOtpService()
        {
            var mock = new Mock<IOtpService>();
            mock.Setup(x => x.InitiateRegistrationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid().ToString());
            mock.Setup(x => x.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, null, new OtpCacheDto { Email = "test@wasl.com" }));
            mock.Setup(x => x.InitiatePasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid().ToString());
            return mock;
        }

        public static DriverProfile CreateTestDriverProfile(string userId, DriverApprovalStatus status = DriverApprovalStatus.Pending)
        {
            return new DriverProfile
            {
                Id = 1,
                UserId = userId,
                VehicleModel = "Toyota Camry",
                VehicleYear = 2020,
                VinNumber = "1HGCM82633A004352",
                ApprovalStatus = status,
                AverageRating = 0,
                TotalReviews = 0
            };
        }
    }

    internal class TestAsyncQueryProvider : IAsyncQueryProvider
    {
        public static readonly TestAsyncQueryProvider Instance = new();

        private TestAsyncQueryProvider() { }

        public IQueryable CreateQuery(Expression expression)
            => (IQueryable)Activator.CreateInstance(
                typeof(AsyncQueryable<>).MakeGenericType(expression.Type.GetGenericArguments()[0]),
                expression, this)!;

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new AsyncQueryable<TElement>(expression, this);

        public object? Execute(Expression expression)
            => EvaluateExpression(expression);

        public TResult Execute<TResult>(Expression expression)
            => (TResult)EvaluateExpression(expression)!;

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
            => new AsyncQueryable<TResult>(expression, this);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var result = EvaluateExpression(expression);

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                if (result is Task taskResult)
                    return (TResult)(object)taskResult;

                var innerType = typeof(TResult).GetGenericArguments()[0];
                if (result == null || innerType.IsInstanceOfType(result))
                {
                    var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!
                        .MakeGenericMethod(innerType);
                    return (TResult)fromResult.Invoke(null, new[] { result })!;
                }
            }

            return (TResult)result!;
        }

        private static object? EvaluateExpression(Expression expression)
        {
            var lambda = Expression.Lambda(expression);
            try
            {
                var compiled = lambda.Compile();
                return compiled.DynamicInvoke();
            }
            catch (TargetInvocationException ex) when (
                ex.InnerException is InvalidOperationException ioe &&
                (ioe.Message.Contains("IAsyncQueryProvider") || ioe.Message.Contains("ExecuteDelete") || ioe.Message.Contains("ExecuteUpdate")))
            {
                return 0;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        }
    }

    internal class AsyncQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly Expression _expression;
        private readonly TestAsyncQueryProvider _provider;

        internal AsyncQueryable(Expression expression, TestAsyncQueryProvider provider)
        {
            _expression = expression;
            _provider = provider;
        }

        public Expression Expression => _expression;
        public Type ElementType => typeof(T);
        public IQueryProvider Provider => _provider;
        public IEnumerator<T> GetEnumerator() => _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(_data.GetEnumerator());

        private IEnumerable<T> _data
        {
            get
            {
                var result = _provider.Execute<IEnumerable<T>>(_expression);
                return result ?? Enumerable.Empty<T>();
            }
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
        public T Current => _inner.Current;
        public ValueTask DisposeAsync() { _inner.Dispose(); return new ValueTask(); }
    }
}
