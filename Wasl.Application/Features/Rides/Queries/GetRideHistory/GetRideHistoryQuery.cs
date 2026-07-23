using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;

namespace Wasl.Application.Features.Rides.Queries.GetRideHistory
{
    public class GetRideHistoryQuery : IRequest<ApiResponse<PagedList<RideHistoryDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
