using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Admin.Queries.GetAllDrivers
{
    public class GetAllDriversQuery : IRequest<ApiResponse<PagedList<DriverListDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; } 
        public DriverApprovalStatus? StatusFilter { get; set; }
    }
}