using FluentValidation;
using MediatR;
using Wasl.Application.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wasl.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

                if (failures.Count != 0)
                {
                    var errorsDictionary = failures
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.ErrorMessage).ToList()
                        );

                    var responseType = typeof(TResponse);
                    if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
                    {
                        var resultType = responseType.GetGenericArguments()[0];
                        var failureMethod = typeof(ApiResponse<>)
                            .MakeGenericType(resultType)
                            .GetMethod("Failure", new[] { typeof(string), typeof(Dictionary<string, List<string>>) });

                     
                        return (TResponse)failureMethod.Invoke(null, new object[] { "Validation Errors Occurred.", errorsDictionary });
                    }

                    throw new ValidationException(failures);
                }
            }
            return await next();
        }
    }
}