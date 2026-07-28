using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Interfaces.Infrastructure;

namespace Wasl.Infrastructure.Services
{
    public class MockPaymentGatewayService : IPaymentGatewayService
    {
        private static readonly ConcurrentDictionary<string, string> _cardTokens = new();

        public async Task<string?> TokenizeCardAsync(CardDetails card, string userId, CancellationToken cancellationToken)
        {
            await Task.Delay(500, cancellationToken);

            var prefix = card.CardNumber.Length >= 4 ? card.CardNumber[..4] : card.CardNumber;

            if (prefix is not ("4242" or "5555" or "1111"))
                return null;

            var token = Guid.CreateVersion7().ToString();
            _cardTokens[token] = card.CardNumber;
            return token;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(string cardToken, decimal amount, string userId, CancellationToken cancellationToken)
        {
            if (!_cardTokens.TryRemove(cardToken, out var cardNumber))
                return new PaymentResult(false, null, "Invalid or expired payment token.");

            await Task.Delay(2000, cancellationToken);

            var prefix = cardNumber.Length >= 4 ? cardNumber[..4] : cardNumber;

            return prefix switch
            {
                "4242" => new PaymentResult(true, Guid.CreateVersion7().ToString(), null),
                "5555" => new PaymentResult(false, null, "Card declined: Insufficient funds."),
                "1111" => new PaymentResult(false, null, "Card declined: Expired card."),
                _ => new PaymentResult(false, null, "Invalid card number.")
            };
        }
    }
}
