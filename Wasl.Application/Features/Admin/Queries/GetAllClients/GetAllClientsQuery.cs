using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;

namespace Wasl.Application.Features.Admin.Queries.GetAllClients
{
    public class GetAllClientsQuery : IRequest<ApiResponse<PagedList<ClientListDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }
}
