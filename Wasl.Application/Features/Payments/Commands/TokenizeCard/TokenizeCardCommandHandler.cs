using MediatR;
using Microsoft.Extensions.Localization;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Payments.Commands.TokenizeCard
{
    public class TokenizeCardCommandHandler : IRequestHandler<TokenizeCardCommand, ApiResponse<string>>
    {
        private readonly IPaymentGatewayService _paymentGateway;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public TokenizeCardCommandHandler(
            IPaymentGatewayService paymentGateway,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _paymentGateway = paymentGateway;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<string>> Handle(TokenizeCardCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<string>.Failure("Unauthorized access.");

            var cardDetails = new CardDetails(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Cvv);

            var token = await _paymentGateway.TokenizeCardAsync(cardDetails, userId, cancellationToken);

            if (token is null)
                return ApiResponse<string>.Failure(_localizer["Payments.InvalidCard"]);

            return ApiResponse<string>.Success(token, _localizer["Payments.TokenizeSuccess"]);
        }
    }
}
