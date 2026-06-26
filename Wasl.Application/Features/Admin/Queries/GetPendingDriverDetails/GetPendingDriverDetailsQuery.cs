using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;

namespace Wasl.Application.Features.Admin.Queries.GetPendingDriverDetails
{
    public class GetPendingDriverDetailsQuery : IRequest<ApiResponse<PendingDriverDetailsDto>>
    {
        public string DriverId { get; set; } = string.Empty;
    }
}