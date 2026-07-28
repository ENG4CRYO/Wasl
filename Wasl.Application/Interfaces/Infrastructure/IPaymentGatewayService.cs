using System.Threading;
using System.Threading.Tasks;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public record PaymentResult(bool IsSuccess, string? TransactionId, string? ErrorMessage);

    public record CardDetails(string CardNumber, string ExpiryMonth, string ExpiryYear, string Cvv);

    public interface IPaymentGatewayService
    {
        Task<string?> TokenizeCardAsync(CardDetails card, string userId, CancellationToken cancellationToken);
        Task<PaymentResult> ProcessPaymentAsync(string cardToken, decimal amount, string userId, CancellationToken cancellationToken);
    }
}
