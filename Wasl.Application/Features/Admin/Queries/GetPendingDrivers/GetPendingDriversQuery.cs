using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;

namespace Wasl.Application.Features.Admin.Queries.GetPendingDrivers
{
    public class GetPendingDriversQuery : IRequest<ApiResponse<PagedList<PendingDriverListDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}