using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Constants;

namespace Wasl.Application.Features.Admin.Queries.GetAllClients
{
    public class GetAllClientsQueryHandler : IRequestHandler<GetAllClientsQuery, ApiResponse<PagedList<ClientListDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetAllClientsQueryHandler(IApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<ApiResponse<PagedList<ClientListDto>>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
        {
            var riderRoleId = await _context.Roles
                .Where(r => r.Name == AspRoles.Rider)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var userIdsInRole = _context.UserRoles
                .Where(ur => ur.RoleId == riderRoleId)
                .Select(ur => ur.UserId);

            var query = _context.Users
                .Where(u => userIdsInRole.Contains(u.Id))
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                string search = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(search) ||
                    u.LastName.ToLower().Contains(search) ||
                    u.PhoneNumber!.Contains(search) ||
                    u.Email!.ToLower().Contains(search));
            }

            query = query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);

            var mappedQuery = query.Select(u => new ClientListDto
            {
                ClientId = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber ?? "",
                Balance = u.Balance
            });

            var pagedClients = await PagedList<ClientListDto>.CreateAsync(
                mappedQuery,
                request.PageNumber,
                request.PageSize);

            return ApiResponse<PagedList<ClientListDto>>.Success(pagedClients, _localizer["Admin.ClientsRetrievedSuccessfully"]);
        }
    }
}
