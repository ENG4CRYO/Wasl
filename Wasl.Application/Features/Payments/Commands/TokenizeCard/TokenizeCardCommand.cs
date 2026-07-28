using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Payments.Commands.TokenizeCard
{
    public class TokenizeCardCommand : IRequest<ApiResponse<string>>
    {
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
    }
}
